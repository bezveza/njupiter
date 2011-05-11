﻿#region Copyright & License
/*
	Copyright (c) 2005-2011 nJupiter

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace nJupiter.Configuration {
	internal class FileConfigLoader : IConfigLoader {
		
		private static readonly char[] IllegalPathCharacters = new[] { '\\', '/', '"', '?', '<', '>' };
		private readonly string configSuffix;
		private readonly bool addFileWatchers;
		private readonly IEnumerable<string> configPaths;
		private readonly ConfigSourceFactory configSourceFactory;

		public FileConfigLoader(IConfig config, ConfigSourceFactory configSourceFactory) {
			this.configSourceFactory = configSourceFactory;
			this.configPaths = GetConfigPaths(config);
			this.configSuffix = GetConfigSuffix(config);
			this.addFileWatchers = GetConfigAddFileWatchers(config);
		}

		public ConfigCollection LoadAll() {
			return LoadConfigs("*");
		}

		public IConfig Load(string configKey) {
			ConfigCollection configs = LoadConfigs(configKey);
			if(configs.Contains(configKey)) {
				return configs[configKey];
			}
			return null;
		}

		private ConfigCollection LoadConfigs(string pattern) {
			try {
				return this.LoadConfigsFromFiles(pattern);
			}catch(Exception ex){
				throw new ConfiguratorException(string.Format("Error while loading XML configuration for the config with pattern [{0}].", pattern), ex);
			}
		}

		private ConfigCollection LoadConfigsFromFiles(string pattern) {
			ConfigCollection configs = new ConfigCollection();
			if(pattern.IndexOfAny(IllegalPathCharacters) < 0) {
				IEnumerable<FileInfo> files = GetFiles(pattern);
				foreach(FileInfo file in files) {
					string configKey = file.Name.Substring(0, file.Name.Length - configSuffix.Length);
					IConfig config = CreateConfigFromFile(configKey, file);
					configs.Insert(config);
				}
			}
			return configs;
		}

		private IEnumerable<FileInfo> GetFiles(string pattern) {
			List<FileInfo> files = new List<FileInfo>();
			foreach(string path in this.configPaths) {
				DirectoryInfo dir = GetDirectory(path);
				if(dir.Exists) {
					FileInfo[] fileArray = dir.GetFiles(string.Format("{0}{1}", pattern, this.configSuffix));
					files.AddRange(fileArray);
				}
			}
			return files;
		}

		private IConfig CreateConfigFromFile(string configKey, FileInfo configFile) {
			if(configFile.Name.StartsWith(configKey) && File.Exists(configFile.FullName)) {
				using(Stream stream = OpenFile(configFile)) {
					return CreateConfigFromStream(configFile, configKey, stream);
				}
			}
			return null;
		}

		private IConfig CreateConfigFromStream(FileInfo configFile, string configKey, Stream stream) {
			IConfigSource source = this.configSourceFactory.CreateConfigSource(configFile, this.addFileWatchers);
			var config = ConfigFactory.Create(configKey, stream, source);
			return config;
		}

		// Internal for test purposes
		internal static Stream OpenFile(FileInfo configFile) {
			Exception exception = null;
			for(int retries = 5; retries >= 0; retries--) {
				try {
					return configFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
				} catch(IOException ex) {
					exception = ex;
					System.Threading.Thread.Sleep(250);
				}
			}
			throw new ConfiguratorException(string.Format("Failed to open XML config file [{0}].", configFile.Name), exception);
		}

		private static DirectoryInfo GetDirectory(string path) {
			if(HttpContext.Current != null && path.StartsWith("~")) {
				path = HttpContext.Current.Server.MapPath(path);
			}
			return new DirectoryInfo(path);
		}

		private static bool GetConfigAddFileWatchers(IConfig config) {
			if(config != null && config.ContainsAttribute("configDirectories", "fileWatchingDisabled")) {
				return !config.GetAttribute<bool>("configDirectories", "fileWatchingDisabled");
			}
			return true;
		}

		private static string GetConfigSuffix(IConfig config) {
			if(config != null && config.ContainsAttribute("configDirectories", "configSuffix")) {
				return config.GetAttribute("configDirectories", "configSuffix");
			}
			return ".config";
		}

		private static IEnumerable<string> GetConfigPaths(IConfig config) {
			string[] paths = config != null ? config.GetValueArray("configDirectories", "configDirectory") : null;
			if(paths == null || paths.Length == 0) {
				paths = Directory.GetDirectories(GetCurrentDirectory(), "*", SearchOption.AllDirectories);
			}
			return paths;
		}

		private static string GetCurrentDirectory() {
			if(HttpContext.Current != null) {
				return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath;
			}
			return Environment.CurrentDirectory;
		}
	}
}
