using System;
using System.Linq;
using System.Web.Security;

using nJupiter.Abstraction.Logging;
using nJupiter.DataAccess.Ldap.Configuration;
using nJupiter.DataAccess.Ldap.DirectoryServices.Abstractions;

namespace nJupiter.DataAccess.Ldap.DirectoryServices {
	internal class MembershipAdapter {

		private readonly ILdapConfig configuration;
		private readonly IMembershipUserFactory membershipUserFactory;
		private readonly IUserEntryAdapter userEntryAdapter;
		private readonly ILog<MembershipAdapter> log;

		public MembershipAdapter(ILdapConfig configuration,
		                         IMembershipUserFactory membershipUserFactory) {

			this.configuration = configuration;
			this.membershipUserFactory = membershipUserFactory;
			userEntryAdapter = configuration.Container.UserEntryAdapter;
			log = configuration.Container.LogManager.GetLogger<MembershipAdapter>();
		}

		public bool ValidateUser(string username, string password) {
			try {
				using(var user = userEntryAdapter.GetUserEntry(username, password)) {
					return user.GetProperties(configuration.Users.RdnAttribute).Any();
				}
			} catch(Exception ex) {
				log.Debug(c => c(string.Format("Failed to validate user '{0}'.", username), ex));
			}
			return false;
		}

		public MembershipUser GetUser(object providerUserKey, bool userIsOnline) {
			if(providerUserKey == null) {
				throw new ArgumentNullException("providerUserKey");
			}
			var username = providerUserKey.ToString();
			return GetUser(username, userIsOnline);
		}

		public MembershipUser GetUser(string username, bool userIsOnline) {
			using(var user = userEntryAdapter.GetUserEntry(username, true)) {
				return membershipUserFactory.Create(user);
			}
		}

		public string GetUserNameByEmail(string email) {
			using(var user = userEntryAdapter.GetUserEntryByEmail(email)) {
				var membershipUser = membershipUserFactory.Create(user);
				return membershipUser != null ? membershipUser.UserName : null;
			}
		}

		public MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords) {
			using(var results = userEntryAdapter.GetAllUserEntries(pageIndex, pageSize, out totalRecords)) {
				return membershipUserFactory.CreateCollection(results);
			}
		}


		public MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords) {
			using(var results = userEntryAdapter.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords)) {
				return membershipUserFactory.CreateCollection(results);
			}
		}

		public MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords) {
			using(var results = userEntryAdapter.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords)) {
				return membershipUserFactory.CreateCollection(results);
			}
		}
		
	}
}