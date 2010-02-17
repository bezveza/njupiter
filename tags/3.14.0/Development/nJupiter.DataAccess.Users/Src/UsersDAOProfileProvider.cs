#region Copyright & License
/*
	Copyright (c) 2005-2010 nJupiter

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
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Web.Profile;

namespace nJupiter.DataAccess.Users {

	public class UsersDAOProfileProvider : ProfileProvider {

		#region Fields
		private string		appName;
		private string		providerName;
		private UsersDAO	usersDAO;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the UsersDAO instance associated with this provider.
		/// </summary>
		/// <value>The UsersDAO instance associated with this provider.</value>
		public UsersDAO UsersDAO {
			get {
				return this.usersDAO;
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Gets the providerName from the username oa a membership user
		/// </summary>
		/// <param providerName="membershipUserName">Name of the membership user.</param>
		/// <returns></returns>
		protected static string GetUserNameFromMembershipUserName(string membershipUserName) {
			if(membershipUserName.Contains("\\")){
				return membershipUserName.Substring(membershipUserName.IndexOf("\\") + 1);
			}
			return membershipUserName;
		}

		/// <summary>
		/// Gets the domain from the username oa a membership user.
		/// </summary>
		/// <param providerName="membershipUserName">Domain of the membership user.</param>
		/// <returns></returns>
		protected static string GetDomainFromMembershipUserName(string membershipUserName) {
			if(membershipUserName.Contains("\\")){
				return membershipUserName.Substring(0, membershipUserName.IndexOf("\\"));
			}
			return null;
		}
		#endregion

		#region Private Methods
		private static string GetStringConfigValue(NameValueCollection config, string configKey, string defaultValue) {
			if((config != null) && (config[configKey] != null)) {
				return config[configKey];
			}
			return defaultValue;
		}

		private AbstractProperty GetAbstractProperty(User user, string propertyName) {
			string contextName = this.UsersDAO.PropertyNames.GetContextName(propertyName);
			if(!string.IsNullOrEmpty(contextName)) {
				Context context = this.UsersDAO.GetContext(contextName);
				return user.Properties[propertyName, context];
			}
			return user.Properties[propertyName];
		}
		#endregion

		/// <summary>Retrieves profile property information and values from a UserDAO profile.</summary>
		/// <returns>A <see cref="T:System.Configuration.SettingsPropertyValueCollection"></see> containing profile property information and values.</returns>
		/// <param providerName="properties">A <see cref="T:System.Configuration.SettingsPropertyCollection"></see> containing profile information for the properties to be retrieved.</param>
		/// <param providerName="sc">The <see cref="T:System.Configuration.SettingsContext"></see> that contains user profile information.</param>
		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext sc, SettingsPropertyCollection properties) {
			SettingsPropertyValueCollection svc = new SettingsPropertyValueCollection();
			if(properties.Count >= 1) {
				string username = (string)sc["UserName"];
				foreach(SettingsProperty property in properties) {
					if(property.SerializeAs == SettingsSerializeAs.ProviderSpecific) {
						if(property.PropertyType.IsPrimitive || (property.PropertyType == typeof(string))) {
							property.SerializeAs = SettingsSerializeAs.String;
						} else {
							property.SerializeAs = SettingsSerializeAs.Xml;
						}
					}
					svc.Add(new SettingsPropertyValue(property));
				}
				if(!string.IsNullOrEmpty(username)) {
					string name = GetUserNameFromMembershipUserName(username);
					string domain = GetDomainFromMembershipUserName(username);
					User user = this.UsersDAO.GetUserByUserName(name, domain);
					if(user == null) {
						user = this.UsersDAO.CreateUserInstance(name, domain);
						this.UsersDAO.SetPassword(user, Guid.NewGuid().ToString("N"));
						this.UsersDAO.SaveUser(user);
					}
					if(user != null) {
						foreach(SettingsPropertyValue sv in svc) {
							AbstractProperty abstractProperty = GetAbstractProperty(user, sv.Name);
							if(abstractProperty != null) {
								sv.PropertyValue = abstractProperty.Value;
								sv.IsDirty = false;
								sv.Deserialized = true;
							}
						}
					}
				}
			}
			return svc;
		}

		/// <summary>Updates the UsersDAO profile with the specified property values.</summary>
		/// <param providerName="properties">A <see cref="T:System.Configuration.SettingsPropertyValueCollection"></see> containing profile information and values for the properties to be updated.</param>
		/// <param providerName="sc">The <see cref="T:System.Configuration.SettingsContext"></see> that contains user profile information.</param>
		public override void SetPropertyValues(SettingsContext sc, SettingsPropertyValueCollection properties) {
			string username = (string)sc["UserName"];
			bool isIsAuthenticated = (bool)sc["IsAuthenticated"];
			if(isIsAuthenticated && !string.IsNullOrEmpty(username) && properties.Count > 0) {
				string name = GetUserNameFromMembershipUserName(username);
				string domain = GetDomainFromMembershipUserName(username);
				User user = this.UsersDAO.GetUserByUserName(name, domain);
				if(user != null) {
					bool userIsDirty = false;
					foreach(SettingsPropertyValue propertyValue in properties) {
						AbstractProperty abstractProperty = GetAbstractProperty(user, propertyValue.Name);
						if(abstractProperty != null) {
							if(propertyValue.IsDirty) {
								abstractProperty.Value = propertyValue.PropertyValue;
								userIsDirty |= abstractProperty.IsDirty;
							}
						} else {
							//TODO: G�r s� man dynamiskt i userdaoen kan l�gga dit property schema definitioner
							throw new ProviderException(string.Format("UsersDAO {0} is not configured to handle a property with the providerName {1}", this.UsersDAO.Name, propertyValue.Name));
						}
					}
					if(userIsDirty) {
						this.UsersDAO.SaveUser(user);
					}
				}
			}
		}

		/// <summary>
		/// Initializes the provider.
		/// </summary>
		/// <param providerName="name">The friendly providerName of the provider.</param>
		/// <param providerName="config">A collection of the providerName/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// The providerName of the provider is null.
		/// </exception>
		/// <exception cref="T:System.ArgumentException">
		/// The providerName of the provider has a length of zero.
		/// </exception>
		/// <exception cref="T:System.InvalidOperationException">
		/// An attempt is made to call <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> on a provider after the provider has already been initialized.
		/// </exception>
		public override void Initialize(string name, NameValueCollection config) {
			if(config == null) {
				throw new ArgumentNullException("config");
			}
			string provider = UsersDAOProfileProvider.GetStringConfigValue(config, "userDAO", string.Empty);
			this.usersDAO = string.IsNullOrEmpty(provider) ? UsersDAO.GetInstance() : UsersDAO.GetInstance(provider);
			this.providerName = !string.IsNullOrEmpty(name) ? name : this.usersDAO.Name;
			base.Initialize(this.providerName, config);
			this.appName = UsersDAOProfileProvider.GetStringConfigValue(config, "applicationName", this.usersDAO.Name);
		}

		/// <summary>
		/// Gets or sets the providerName of the currently running application.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A <see cref="T:System.String"/> that contains the application's shortened providerName, which does not contain a full path or extension, for example, SimpleAppSettings.
		/// </returns>
		public override string ApplicationName {
			get {
				return this.appName;
			}
			set {
				if(value.Length > 256) {
					throw new ProviderException("Provider application providerName too long");
				}
				this.appName = value;
			}
		}

		/// <summary>
		/// When overridden in a derived class, deletes profile properties and information for the supplied list of profiles.
		/// </summary>
		/// <param providerName="profiles">A <see cref="T:System.Web.Profile.ProfileInfoCollection"/>  of information about profiles that are to be deleted.</param>
		/// <returns>
		/// The number of profiles deleted from the data source.
		/// </returns>
		public override int DeleteProfiles(ProfileInfoCollection profiles) {
			if(profiles == null) {
				throw new ArgumentNullException("profiles");
			}
			if(profiles.Count < 1) {
				throw new ArgumentException("Parameter collection empty", "profiles");
			}
			string[] usernames = new string[profiles.Count];
			int count = 0;
			foreach(ProfileInfo info1 in profiles) {
				usernames[count++] = info1.UserName;
			}
			return this.DeleteProfiles(usernames);
		}

		/// <summary>
		/// When overridden in a derived class, deletes profile properties and information for profiles that match the supplied list of user names.
		/// </summary>
		/// <param providerName="usernames">A string array of user names for profiles to be deleted.</param>
		/// <returns>
		/// The number of profiles deleted from the data source.
		/// </returns>
		public override int DeleteProfiles(string[] usernames) {
			int count = 0;
			foreach(string username in usernames) {
				string name = GetUserNameFromMembershipUserName(username);
				string domain = GetDomainFromMembershipUserName(username);
				User user = this.UsersDAO.GetUserByUserName(name, domain);
				if(user != null) {
					this.UsersDAO.DeleteUser(user);
					count++;
				}
			}
			return count;
		}

		/// <summary>
		/// When overridden in a derived class, deletes all user-profile data for profiles in which the last activity date occurred before the specified date.
		/// </summary>
		/// <param providerName="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption"/> values, specifying whether anonymous, authenticated, or both types of profiles are deleted.</param>
		/// <param providerName="userInactiveSinceDate">A <see cref="T:System.DateTime"/> that identifies which user profiles are considered inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate"/>  value of a user profile occurs on or before this date and time, the profile is considered inactive.</param>
		/// <returns>
		/// The number of profiles deleted from the data source.
		/// </returns>
		public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate) {
			return 0;
		}

		/// <summary>
		/// When overridden in a derived class, returns the number of profiles in which the last activity date occurred on or before the specified date.
		/// </summary>
		/// <param providerName="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption"/> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
		/// <param providerName="userInactiveSinceDate">A <see cref="T:System.DateTime"/> that identifies which user profiles are considered inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate"/>  of a user profile occurs on or before this date and time, the profile is considered inactive.</param>
		/// <returns>
		/// The number of profiles in which the last activity date occurred on or before the specified date.
		/// </returns>
		public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate) {
			return 0;
		}

		/// <summary>
		/// When overridden in a derived class, retrieves user profile data for all profiles in the data source.
		/// </summary>
		/// <param providerName="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption"/> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
		/// <param providerName="pageIndex">The index of the page of results to return.</param>
		/// <param providerName="pageSize">The size of the page of results to return.</param>
		/// <param providerName="totalRecords">When this method returns, contains the total number of profiles.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Profile.ProfileInfoCollection"/> containing user-profile information for all profiles in the data source.
		/// </returns>
		public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex, int pageSize, out int totalRecords) {
			ProfileInfoCollection pic = new ProfileInfoCollection();
			totalRecords = 0;
			if(!authenticationOption.Equals(ProfileAuthenticationOption.Anonymous)) {
				UserCollection uc = this.UsersDAO.GetAllUsers(pageIndex, pageSize, out totalRecords);
				foreach(User user in uc) {
					string username = string.IsNullOrEmpty(user.Domain) ? user.UserName : string.Format("{0}\\{1}", user.Domain, user.UserName);
					pic.Add(new ProfileInfo(username, user.Properties.IsAnonymous, user.Properties.LastActivityDate, user.Properties.LastUpdatedDate, 0));
				}
				totalRecords = pic.Count;
			}
			return pic;
		}

		/// <summary>
		/// When overridden in a derived class, retrieves user-profile data from the data source for profiles in which the last activity date occurred on or before the specified date.
		/// </summary>
		/// <param providerName="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption"/> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
		/// <param providerName="userInactiveSinceDate">A <see cref="T:System.DateTime"/> that identifies which user profiles are considered inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate"/>  of a user profile occurs on or before this date and time, the profile is considered inactive.</param>
		/// <param providerName="pageIndex">The index of the page of results to return.</param>
		/// <param providerName="pageSize">The size of the page of results to return.</param>
		/// <param providerName="totalRecords">When this method returns, contains the total number of profiles.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Profile.ProfileInfoCollection"/> containing user-profile information about the inactive profiles.
		/// </returns>
		public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords) {
			totalRecords = 0;
			return new ProfileInfoCollection();
		}

		/// <summary>
		/// When overridden in a derived class, retrieves profile information for profiles in which the user providerName matches the specified user names.
		/// </summary>
		/// <param providerName="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption"/> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
		/// <param providerName="usernameToMatch">The user providerName to search for.</param>
		/// <param providerName="pageIndex">The index of the page of results to return.</param>
		/// <param providerName="pageSize">The size of the page of results to return.</param>
		/// <param providerName="totalRecords">When this method returns, contains the total number of profiles.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Profile.ProfileInfoCollection"/> containing user-profile information for profiles where the user providerName matches the supplied <paramref providerName="usernameToMatch"/> parameter.
		/// </returns>
		public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, int pageIndex, int pageSize, out int totalRecords) {
			if(usernameToMatch == null)
				throw new ArgumentNullException("usernameToMatch");
			ProfileInfoCollection pic = new ProfileInfoCollection();
			usernameToMatch = usernameToMatch.Replace("%", string.Empty);
			string name = GetUserNameFromMembershipUserName(usernameToMatch);
			string domain = GetDomainFromMembershipUserName(usernameToMatch);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			totalRecords = 0;
			if(user != null) {
				string username = string.IsNullOrEmpty(user.Domain) ? user.UserName : string.Format("{0}\\{1}", user.Domain, user.UserName);
				pic.Add(new ProfileInfo(username, user.Properties.IsAnonymous, user.Properties.LastActivityDate, user.Properties.LastUpdatedDate, 0));
				totalRecords = 1;
			}
			return pic;
		}

		/// <summary>
		/// When overridden in a derived class, retrieves profile information for profiles in which the last activity date occurred on or before the specified date and the user providerName matches the specified user providerName.
		/// </summary>
		/// <param providerName="authenticationOption">One of the <see cref="T:System.Web.Profile.ProfileAuthenticationOption"/> values, specifying whether anonymous, authenticated, or both types of profiles are returned.</param>
		/// <param providerName="usernameToMatch">The user providerName to search for.</param>
		/// <param providerName="userInactiveSinceDate">A <see cref="T:System.DateTime"/> that identifies which user profiles are considered inactive. If the <see cref="P:System.Web.Profile.ProfileInfo.LastActivityDate"/> value of a user profile occurs on or before this date and time, the profile is considered inactive.</param>
		/// <param providerName="pageIndex">The index of the page of results to return.</param>
		/// <param providerName="pageSize">The size of the page of results to return.</param>
		/// <param providerName="totalRecords">When this method returns, contains the total number of profiles.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Profile.ProfileInfoCollection"/> containing user profile information for inactive profiles where the user providerName matches the supplied <paramref providerName="usernameToMatch"/> parameter.
		/// </returns>
		public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords) {
			totalRecords = 0;
			return new ProfileInfoCollection();
		}
	}
}
