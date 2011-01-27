#region Copyright & License
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
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Text.RegularExpressions;
using System.Web.Security;

namespace nJupiter.DataAccess.Users {
	public class UsersDAOMembershipProvider : MembershipProvider {

		#region Fields
		private string providerName;
		private string appName;
		private bool enablePasswordReset = true;
		private bool enablePasswordRetrieval;
		private int maxInvalidPasswordAttempts = 5;
		private int minRequiredNonAlphanumericCharacters = 1;
		private int minRequiredPasswordLength = 5;
		private int passwordAttemptWindow = 10;
		private MembershipPasswordFormat passwordFormat = MembershipPasswordFormat.Hashed;
		private string passwordStrengthRegularExpression = string.Empty;
		private bool requiresQuestionAndAnswer;
		private bool requiresUniqueEmail;
		private UsersDAO usersDAO;
		#endregion

		#region Properties
		/// <summary>
		/// The name of the application using the custom membership provider.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The name of the application using the custom membership provider.
		/// </returns>
		public override string ApplicationName { get { return this.appName; } set { this.appName = value; } }
		/// <summary>
		/// Indicates whether the membership provider is configured to allow users to retrieve their passwords.
		/// </summary>
		/// <value></value>
		/// <returns>true if the membership provider is configured to support password retrieval; otherwise, false. The default is false.
		/// </returns>
		public override bool EnablePasswordRetrieval { get { return this.enablePasswordRetrieval; } }
		/// <summary>
		/// Indicates whether the membership provider is configured to allow users to reset their passwords.
		/// </summary>
		/// <value></value>
		/// <returns>true if the membership provider supports password reset; otherwise, false. The default is true.
		/// </returns>
		public override bool EnablePasswordReset { get { return this.enablePasswordReset; } }
		/// <summary>
		/// Gets a value indicating whether the membership provider is configured to require the user to answer a password question for password reset and retrieval.
		/// </summary>
		/// <value></value>
		/// <returns>true if a password answer is required for password reset and retrieval; otherwise, false. The default is true.
		/// </returns>
		public override bool RequiresQuestionAndAnswer { get { return this.requiresQuestionAndAnswer; } }
		/// <summary>
		/// Gets the number of invalid password or password-answer attempts allowed before the membership user is locked out.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The number of invalid password or password-answer attempts allowed before the membership user is locked out.
		/// </returns>
		public override int MaxInvalidPasswordAttempts { get { return this.maxInvalidPasswordAttempts; } }
		/// <summary>
		/// Gets the number of minutes in which a maximum number of invalid password or password-answer attempts are allowed before the membership user is locked out.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The number of minutes in which a maximum number of invalid password or password-answer attempts are allowed before the membership user is locked out.
		/// </returns>
		public override int PasswordAttemptWindow { get { return this.passwordAttemptWindow; } }
		/// <summary>
		/// Gets a value indicating whether the membership provider is configured to require a unique e-mail address for each user name.
		/// </summary>
		/// <value></value>
		/// <returns>true if the membership provider requires a unique e-mail address; otherwise, false. The default is true.
		/// </returns>
		public override bool RequiresUniqueEmail { get { return this.requiresUniqueEmail; } }
		/// <summary>
		/// Gets a value indicating the format for storing passwords in the membership data store.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Web.Security.MembershipPasswordFormat"/> values indicating the format for storing passwords in the data store.
		/// </returns>
		public override MembershipPasswordFormat PasswordFormat { get { return this.passwordFormat; } }
		/// <summary>
		/// Gets the minimum length required for a password.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The minimum length required for a password.
		/// </returns>
		public override int MinRequiredPasswordLength { get { return this.minRequiredPasswordLength; } }
		/// <summary>
		/// Gets the minimum number of special characters that must be present in a valid password.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The minimum number of special characters that must be present in a valid password.
		/// </returns>
		public override int MinRequiredNonAlphanumericCharacters { get { return this.minRequiredNonAlphanumericCharacters; } }
		/// <summary>
		/// Gets the regular expression used to evaluate a password.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A regular expression used to evaluate a password.
		/// </returns>
		public override string PasswordStrengthRegularExpression { get { return this.passwordStrengthRegularExpression; } }
		/// <summary>
		/// Gets the friendly name used to refer to the provider during configuration.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The friendly name used to refer to the provider during configuration.
		/// </returns>
		public override string Name {
			get {
				return string.IsNullOrEmpty(this.providerName) ? this.GetType().Name : this.providerName;
			}
		}
		/// <summary>
		/// Gets the UsersDAO instance associated with this membership provider.
		/// </summary>
		/// <value>The UsersDAO instance associated with this membership provider.</value>
		public UsersDAO UsersDAO {
			get {
				return this.usersDAO;
			}
		}
		#endregion

		#region Private methods
		private static bool GetBooleanConfigValue(NameValueCollection config, string configKey, bool defaultValue) {
			bool result = defaultValue;
			if((config != null) && (config[configKey] != null)) {
				bool.TryParse(config[configKey], out result);
			}
			return result;
		}

		private static int GetIntegerConfigValue(NameValueCollection config, string configKey, int defaultValue) {
			int num;
			if(((config != null) && (config[configKey] != null)) && int.TryParse(config[configKey], out num)) {
				return num;
			}
			return defaultValue;
		}

		private static string GetStringConfigValue(NameValueCollection config, string configKey, string defaultValue) {
			if((config != null) && (config[configKey] != null)) {
				return config[configKey];
			}
			return defaultValue;
		}

		private static void CheckParameter(string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName) {
			if(param == null) {
				if(checkForNull) {
					throw new ArgumentNullException(paramName);
				}
			} else {
				param = param.Trim();
				if(checkIfEmpty && (param.Length < 1)) {
					throw new ArgumentException(string.Format("Parameter {0} can not be empty", paramName));
				}
				if((maxSize > 0) && (param.Length > maxSize)) {
					throw new ArgumentException(string.Format("Parameter {0} too long. Maxlength {1}", paramName, maxSize));
				}
				if(checkForCommas && param.Contains(",")) {
					throw new ArgumentException(string.Format("Parameter {0} can not contain comma", paramName));
				}
			}
		}

		private static bool ValidateParameter(string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize) {
			if(param == null) {
				return !checkForNull;
			}
			param = param.Trim();
			if(((!checkIfEmpty || (param.Length >= 1)) && ((maxSize <= 0) || (param.Length <= maxSize))) && (!checkForCommas || !param.Contains(","))) {
				return true;
			}
			return false;
		}

		private void UpdateUserDAOUser(User user) {
			if(user == null) {
				throw new ArgumentNullException("user");
			}
			user.Properties.LastUpdatedDate = DateTime.UtcNow;
			this.UsersDAO.SaveUser(user);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Gets the name from the username oa a membership user
		/// </summary>
		/// <param name="membershipUserName">Name of the membership user.</param>
		/// <returns></returns>
		protected static string GetUserNameFromMembershipUserName(string membershipUserName) {
			if(membershipUserName.Contains("\\")) {
				return membershipUserName.Substring(membershipUserName.IndexOf("\\") + 1);
			}
			return membershipUserName;
		}

		/// <summary>
		/// Gets the domain from the username oa a membership user.
		/// </summary>
		/// <param name="membershipUserName">Domain of the membership user.</param>
		/// <returns></returns>
		protected static string GetDomainFromMembershipUserName(string membershipUserName) {
			if(membershipUserName.Contains("\\")) {
				return membershipUserName.Substring(0, membershipUserName.IndexOf("\\"));
			}
			return null;
		}

		/// <summary>
		/// Processes a request to update the password for a membership user.
		/// </summary>
		/// <param name="username">The user to update the password for.</param>
		/// <param name="oldPassword">The current password for the specified user.</param>
		/// <param name="newPassword">The new password for the specified user.</param>
		/// <param name="doChecks">Do checks on the password agaist rules and the old password.</param>
		/// <returns>
		/// true if the password was updated successfully; otherwise, false.
		/// </returns>
		protected bool ChangePassword(string username, string oldPassword, string newPassword, bool doChecks) {
			if(username == null)
				throw new ArgumentNullException("username");
			UsersDAOMembershipProvider.CheckParameter(username, true, true, true, 256, "username");
			UsersDAOMembershipProvider.CheckParameter(oldPassword, true, true, false, 256, "oldPassword");
			UsersDAOMembershipProvider.CheckParameter(newPassword, true, true, false, 256, "newPassword");

			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user == null) {
				throw new UserDoesNotExistException(string.Format("User with username {0} does not exist.", username));
			}
			if(doChecks) {
				if(newPassword.Length < this.MinRequiredPasswordLength) {
					throw new ArgumentException("Password too short");
				}
				int numberOfNonAlphaChars = 0;
				for(int i = 0; i < newPassword.Length; i++) {
					if(!char.IsLetterOrDigit(newPassword, i)) {
						numberOfNonAlphaChars++;
					}
				}
				if(numberOfNonAlphaChars < this.MinRequiredNonAlphanumericCharacters) {
					throw new ArgumentException("Password need more non_alpha numeric chars");
				}
				if((this.PasswordStrengthRegularExpression.Length > 0) &&
				   !Regex.IsMatch(newPassword, this.PasswordStrengthRegularExpression)) {
					throw new ArgumentException("Password does not match regular expression");
				}
				ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
				this.OnValidatingPassword(args);
				if(args.Cancel) {
					throw new ProviderException("Custom password validation failed.", args.FailureInformation);
				}
				if(!this.UsersDAO.CheckPassword(user, oldPassword))
					return false;
			}
			this.UsersDAO.SetPassword(user, newPassword);
			user.Properties.LastPasswordChangedDate = DateTime.UtcNow;
			this.UpdateUserDAOUser(user);
			return true;
		}

		/// <summary>
		/// Processes a request to update the password question and answer for a membership user.
		/// </summary>
		/// <param name="username">The user to change the password question and answer for.</param>
		/// <param name="password">The password for the specified user.</param>
		/// <param name="newPasswordQuestion">The new password question for the specified user.</param>
		/// <param name="newPasswordAnswer">The new password answer for the specified user.</param>
		/// <param name="doChecks">Do checks on the password agaist rules and the old password.</param>
		/// <returns>
		/// true if the password question and answer are updated successfully; otherwise, false.
		/// </returns>
		protected bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer, bool doChecks) {
			if(username == null)
				throw new ArgumentNullException("username");
			UsersDAOMembershipProvider.CheckParameter(username, true, true, true, 256, "username");
			UsersDAOMembershipProvider.CheckParameter(password, true, true, false, 256, "password");
			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user == null) {
				throw new UserDoesNotExistException(string.Format("User with username {0} does not exist.", username));
			}
			if(doChecks) {
				if(!this.UsersDAO.CheckPassword(user, password)) {
					return false;
				}
				UsersDAOMembershipProvider.CheckParameter(newPasswordQuestion, this.RequiresQuestionAndAnswer, this.RequiresQuestionAndAnswer, false, 256, "newPasswordQuestion");
				UsersDAOMembershipProvider.CheckParameter(newPasswordAnswer, this.RequiresQuestionAndAnswer, this.RequiresQuestionAndAnswer, false, 256, "newPasswordAnswer");
			}
			user.Properties.PasswordQuestion = newPasswordQuestion;
			user.Properties.PasswordAnswer = newPasswordQuestion;
			this.UpdateUserDAOUser(user);
			return true;
		}

		/// <summary>
		/// Verifies that the specified user name and password exist in the data source.
		/// </summary>
		/// <param name="username">The name of the user to validate.</param>
		/// <param name="password">The password for the specified user.</param>
		/// <param name="checkPassword">if set to <c>true</c> then validate the password, else just check if the user exists but still set the LastLoginDate date.</param>
		/// <returns>
		/// true if the specified username and password are valid; otherwise, false.
		/// </returns>
		protected bool ValidateUser(string username, string password, bool checkPassword) {
			if(username == null)
				throw new ArgumentNullException("username");
			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user == null) {
				return false;
			}
			bool passwordCorrect = true;
			if(checkPassword) {
				passwordCorrect = this.UsersDAO.CheckPassword(user, password);
			}
			if(passwordCorrect) {
				user.Properties.LastLoginDate = DateTime.UtcNow;
				this.UpdateUserDAOUser(user);
			}
			return passwordCorrect;
		}
		#endregion

		#region Overridden Methods
		/// <summary>
		/// Initializes the provider.
		/// </summary>
		/// <param name="name">The friendly name of the provider.</param>
		/// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// The name of the provider is null.
		/// </exception>
		/// <exception cref="T:System.ArgumentException">
		/// The name of the provider has a length of zero.
		/// </exception>
		/// <exception cref="T:System.InvalidOperationException">
		/// An attempt is made to call <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> on a provider after the provider has already been initialized.
		/// </exception>
		public override void Initialize(string name, NameValueCollection config) {
			if(config == null) {
				throw new ArgumentNullException("config");
			}
			string provider = UsersDAOMembershipProvider.GetStringConfigValue(config, "userDAO", string.Empty);
			this.usersDAO = string.IsNullOrEmpty(provider) ? UsersDAO.GetInstance() : UsersDAO.GetInstance(provider);

			this.appName = UsersDAOMembershipProvider.GetStringConfigValue(config, "applicationName", this.usersDAO.Name);
			this.providerName = !string.IsNullOrEmpty(name) ? name : this.usersDAO.Name;

			base.Initialize(this.providerName, config);

			this.requiresUniqueEmail = UsersDAOMembershipProvider.GetBooleanConfigValue(config, "requiresUniqueEmail", false);
			this.requiresQuestionAndAnswer = UsersDAOMembershipProvider.GetBooleanConfigValue(config, "requiresQuestionAndAnswer", false);
			this.passwordAttemptWindow = UsersDAOMembershipProvider.GetIntegerConfigValue(config, "passwordAttemptWindow", 10);
			this.minRequiredPasswordLength = UsersDAOMembershipProvider.GetIntegerConfigValue(config, "minRequiredPasswordLength", 7);
			this.minRequiredNonAlphanumericCharacters = UsersDAOMembershipProvider.GetIntegerConfigValue(config, "minRequiredNonalphanumericCharacters", 1);
			this.maxInvalidPasswordAttempts = UsersDAOMembershipProvider.GetIntegerConfigValue(config, "maxInvalidPasswordAttempts", 5);
			this.enablePasswordReset = UsersDAOMembershipProvider.GetBooleanConfigValue(config, "enablePasswordReset", false);
			this.enablePasswordRetrieval = UsersDAOMembershipProvider.GetBooleanConfigValue(config, "enablePasswordRetrieval", false);
			this.passwordStrengthRegularExpression = UsersDAOMembershipProvider.GetStringConfigValue(config, "passwordStrengthRegularExpression", string.Empty);
			this.passwordFormat = (MembershipPasswordFormat)Enum.Parse(typeof(MembershipPasswordFormat), UsersDAOMembershipProvider.GetStringConfigValue(config, "passwordFormat", "Hashed"));
		}

		/// <summary>
		/// Adds a new membership user to the data source.
		/// </summary>
		/// <param name="username">The user name for the new user.</param>
		/// <param name="password">The password for the new user.</param>
		/// <param name="email">The e-mail address for the new user.</param>
		/// <param name="passwordQuestion">The password question for the new user.</param>
		/// <param name="passwordAnswer">The password answer for the new user</param>
		/// <param name="isApproved">Whether or not the new user is approved to be validated.</param>
		/// <param name="providerUserKey">The unique identifier from the membership data source for the user.</param>
		/// <param name="status">A <see cref="T:System.Web.Security.MembershipCreateStatus"/> enumeration value indicating whether the user was created successfully.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the information for the newly created user.
		/// </returns>
		public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status) {
			if(username == null)
				throw new ArgumentNullException("username");
			if(!UsersDAOMembershipProvider.ValidateParameter(password, true, true, false, 256)) {
				status = MembershipCreateStatus.InvalidPassword;
				return null;
			}
			if(!UsersDAOMembershipProvider.ValidateParameter(username, true, true, true, 256)) {
				status = MembershipCreateStatus.InvalidUserName;
				return null;
			}
			if(!UsersDAOMembershipProvider.ValidateParameter(email, this.RequiresUniqueEmail, this.RequiresUniqueEmail, false, 256)) {
				status = MembershipCreateStatus.InvalidEmail;
				return null;
			}
			if(!UsersDAOMembershipProvider.ValidateParameter(passwordQuestion, this.RequiresQuestionAndAnswer, true, false, 256)) {
				status = MembershipCreateStatus.InvalidQuestion;
				return null;
			}
			if(!UsersDAOMembershipProvider.ValidateParameter(passwordAnswer, this.RequiresQuestionAndAnswer, true, false, 256)) {
				status = MembershipCreateStatus.InvalidAnswer;
				return null;
			}
			if(password.Length < this.MinRequiredPasswordLength) {
				status = MembershipCreateStatus.InvalidPassword;
				return null;
			}
			int numberOfNonAlphaChars = 0;
			for(int i = 0; i < password.Length; i++) {
				if(!char.IsLetterOrDigit(password, i)) {
					numberOfNonAlphaChars++;
				}
			}
			if(numberOfNonAlphaChars < this.MinRequiredNonAlphanumericCharacters) {
				status = MembershipCreateStatus.InvalidPassword;
				return null;
			}
			if((this.PasswordStrengthRegularExpression.Length > 0) && !Regex.IsMatch(password, this.PasswordStrengthRegularExpression)) {
				status = MembershipCreateStatus.InvalidPassword;
				return null;
			}
			ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);
			this.OnValidatingPassword(args);
			if(args.Cancel) {
				status = MembershipCreateStatus.InvalidPassword;
				return null;
			}
			try {
				string name = GetUserNameFromMembershipUserName(username);
				string domain = GetDomainFromMembershipUserName(username);
				User user = this.UsersDAO.CreateUserInstance(name, domain);
				this.UsersDAO.SetPassword(user, password);
				user.Properties.Email = email;
				user.Properties.PasswordQuestion = passwordQuestion;
				user.Properties.PasswordAnswer = passwordAnswer;
				user.Properties.Active = isApproved;


				UsersDAOMembershipUser membershipUser = new UsersDAOMembershipUser(user, this.Name);
				this.UpdateUserDAOUser(user);
				status = MembershipCreateStatus.Success;
				return membershipUser;
			} catch(UserNameAlreadyExistsException) {
				status = MembershipCreateStatus.DuplicateUserName;
			} catch(UserNameEmptyException) {
				status = MembershipCreateStatus.InvalidUserName;
			}
			return null;
		}

		/// <summary>
		/// Processes a request to update the password question and answer for a membership user.
		/// </summary>
		/// <param name="username">The user to change the password question and answer for.</param>
		/// <param name="password">The password for the specified user.</param>
		/// <param name="newPasswordQuestion">The new password question for the specified user.</param>
		/// <param name="newPasswordAnswer">The new password answer for the specified user.</param>
		/// <returns>
		/// true if the password question and answer are updated successfully; otherwise, false.
		/// </returns>
		public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer) {
			return ChangePasswordQuestionAndAnswer(username, password, newPasswordQuestion, newPasswordAnswer, true);
		}

		/// <summary>
		/// Processes a request to update the password for a membership user.
		/// </summary>
		/// <param name="username">The user to update the password for.</param>
		/// <param name="oldPassword">The current password for the specified user.</param>
		/// <param name="newPassword">The new password for the specified user.</param>
		/// <returns>
		/// true if the password was updated successfully; otherwise, false.
		/// </returns>
		public override bool ChangePassword(string username, string oldPassword, string newPassword) {
			return ChangePassword(username, oldPassword, newPassword, true);

		}

		/// <summary>
		/// Resets a user's password to a new, automatically generated password.
		/// </summary>
		/// <param name="username">The user to reset the password for.</param>
		/// <param name="passwordAnswer">The password answer for the specified user.</param>
		/// <returns>The new password for the specified user.</returns>
		public override string ResetPassword(string username, string passwordAnswer) {
			if(!this.EnablePasswordReset) {
				throw new NotSupportedException("Not configured to support password resets");
			}
			UsersDAOMembershipProvider.CheckParameter(username, true, true, true, 256, "username");
			UsersDAOMembershipProvider.CheckParameter(passwordAnswer, this.RequiresQuestionAndAnswer, this.RequiresQuestionAndAnswer, false, 256, "passwordAnswer");

			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user == null) {
				throw new UserDoesNotExistException(string.Format("User with username {0} does not exist.", username));
			}
			if(this.RequiresQuestionAndAnswer && (user.Properties.PasswordAnswer == null || !user.Properties.PasswordAnswer.Equals(passwordAnswer, StringComparison.CurrentCultureIgnoreCase))) {
				throw new MembershipPasswordException();
			}
			string newPassword = Membership.GeneratePassword(this.MinRequiredPasswordLength < 6 ? 6 : this.MinRequiredPasswordLength, this.MinRequiredNonAlphanumericCharacters);
			this.UsersDAO.SetPassword(user, newPassword);
			user.Properties.LastPasswordChangedDate = DateTime.UtcNow;
			this.UpdateUserDAOUser(user);
			return newPassword;
		}

		/// <summary>
		/// Updates information about a user in the data source.
		/// </summary>
		/// <param name="user">A <see cref="T:System.Web.Security.MembershipUser"/> object that represents the user to update and the updated information for the user.</param>
		public override void UpdateUser(MembershipUser user) {
			if(user == null) {
				throw new ArgumentNullException("user");
			}
			UsersDAOMembershipProvider.CheckParameter(user.UserName, true, true, true, 256, "UserName");
			UsersDAOMembershipProvider.CheckParameter(user.Email, this.RequiresUniqueEmail, this.RequiresUniqueEmail, false, 256, "Email");

			UsersDAOMembershipUser usersDAOMembershipUser = user as UsersDAOMembershipUser;
			if(usersDAOMembershipUser == null) {
				throw new ArgumentException(string.Format("User is not of type {0}", typeof(UsersDAOMembershipUser).Name), "user");
			}
			User userDaoUser = this.UsersDAO.GetUserById(usersDAOMembershipUser.UserDAOId);
			userDaoUser.Properties.LastLoginDate = user.LastLoginDate;
			userDaoUser.Properties.Description = user.Comment;
			userDaoUser.Properties.Email = user.Email;
			userDaoUser.Properties.Active = user.IsApproved;
			userDaoUser.Properties.LastActivityDate = user.LastActivityDate;
			this.UpdateUserDAOUser(userDaoUser);
		}

		/// <summary>
		/// Verifies that the specified user name and password exist in the data source.
		/// </summary>
		/// <param name="username">The name of the user to validate.</param>
		/// <param name="password">The password for the specified user.</param>
		/// <returns>
		/// true if the specified username and password are valid; otherwise, false.
		/// </returns>
		public override bool ValidateUser(string username, string password) {
			return ValidateUser(username, password, true);
		}

		/// <summary>
		/// Clears a lock so that the membership user can be validated.
		/// </summary>
		/// <param name="username">The membership user whose lock status you want to clear.</param>
		/// <returns>
		/// true if the membership user was successfully unlocked; otherwise, false.
		/// </returns>
		public override bool UnlockUser(string username) {
			if(username == null)
				throw new ArgumentNullException("username");
			UsersDAOMembershipProvider.CheckParameter(username, true, true, true, 0x100, "username");
			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user == null) {
				throw new UserDoesNotExistException(string.Format("User with username {0} does not exist.", username));
			}
			user.Properties.Locked = true;
			this.UpdateUserDAOUser(user);
			return true;
		}

		/// <summary>
		/// Removes a user from the membership data source.
		/// </summary>
		/// <param name="username">The name of the user to delete.</param>
		/// <param name="deleteAllRelatedData">true to delete data related to the user from the database; false to leave data related to the user in the database.</param>
		/// <returns>
		/// true if the user was successfully deleted; otherwise, false.
		/// </returns>
		public override bool DeleteUser(string username, bool deleteAllRelatedData) {
			if(username == null)
				throw new ArgumentNullException("username");
			UsersDAOMembershipProvider.CheckParameter(username, true, true, true, 256, "username");
			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user != null) {
				this.UsersDAO.DeleteUser(user);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets user information from the data source based on the unique identifier for the membership user. Provides an option to update the last-activity date/time stamp for the user.
		/// </summary>
		/// <param name="providerUserKey">The unique identifier for the membership user to get information for.</param>
		/// <param name="userIsOnline">true to update the last-activity date/time stamp for the user; false to return user information without updating the last-activity date/time stamp for the user.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the specified user's information from the data source.
		/// </returns>
		public override MembershipUser GetUser(object providerUserKey, bool userIsOnline) {
			if(providerUserKey == null) {
				throw new ArgumentNullException("providerUserKey");
			}
			User user = this.UsersDAO.GetUserById(providerUserKey.ToString());
			if(user != null) {
				if(userIsOnline) {
					user.Properties.LastActivityDate = DateTime.UtcNow;
					this.UpdateUserDAOUser(user);
				}
				return new UsersDAOMembershipUser(user, this.Name);
			}
			return null;
		}

		/// <summary>
		/// Gets information from the data source for a user. Provides an option to update the last-activity date/time stamp for the user.
		/// </summary>
		/// <param name="username">The name of the user to get information for.</param>
		/// <param name="userIsOnline">true to update the last-activity date/time stamp for the user; false to return user information without updating the last-activity date/time stamp for the user.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the specified user's information from the data source.
		/// </returns>
		public override MembershipUser GetUser(string username, bool userIsOnline) {
			if(username == null) {
				throw new ArgumentNullException("username");
			}
			UsersDAOMembershipProvider.CheckParameter(username, true, false, true, 256, "username");
			string name = GetUserNameFromMembershipUserName(username);
			string domain = GetDomainFromMembershipUserName(username);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			if(user != null) {
				if(userIsOnline) {
					user.Properties.LastUpdatedDate = DateTime.UtcNow;
					this.UpdateUserDAOUser(user);
				}
				return new UsersDAOMembershipUser(user, this.Name);
			}
			return null;
		}

		/// <summary>
		/// Gets the user name associated with the specified e-mail address.
		/// </summary>
		/// <param name="email">The e-mail address to search for.</param>
		/// <returns>
		/// The user name associated with the specified e-mail address. If no match is found, return null.
		/// </returns>
		public override string GetUserNameByEmail(string email) {
			UsersDAOMembershipProvider.CheckParameter(email, false, false, false, 256, "email");
			SearchCriteria sc = new SearchCriteria(this.UsersDAO.PropertyNames.Email, email, true);
			UserCollection uc = this.UsersDAO.GetUsersBySearchCriteria(sc);
			if(this.RequiresUniqueEmail && uc.Count > 1) {
				throw new ProviderException(string.Format("More than one user with email {0}", email));
			}
			if(uc.Count > 0) {
				return uc[0].UserName;
			}
			return null;
		}

		/// <summary>
		/// Gets the number of users currently accessing the application.
		/// </summary>
		/// <returns>
		/// The number of users currently accessing the application.
		/// </returns>
		public override int GetNumberOfUsersOnline() {
			DateTimeProperty lastActivityDateProperty = new DateTimeProperty(this.UsersDAO.PropertyNames.LastActivityDate, null);
			lastActivityDateProperty.Value = DateTime.UtcNow.AddMinutes(-Membership.UserIsOnlineTimeWindow);
			SearchCriteria sc = new SearchCriteria(lastActivityDateProperty, SearchCriteria.CompareCondition.GreaterThan);
			return this.UsersDAO.GetUsersBySearchCriteria(sc).Count;
		}

		/// <summary>
		/// Gets the password for the specified user name from the data source.
		/// </summary>
		/// <param name="username">The user to retrieve the password for.</param>
		/// <param name="passwordAnswer">The password answer for the user.</param>
		/// <returns>
		/// The password for the specified user name.
		/// </returns>
		public override string GetPassword(string username, string passwordAnswer) {
			if(username == null)
				throw new ArgumentNullException("username");
			if(this.EnablePasswordRetrieval) {
				UsersDAOMembershipProvider.CheckParameter(username, true, true, true, 256, "username");
				UsersDAOMembershipProvider.CheckParameter(passwordAnswer, this.RequiresQuestionAndAnswer, this.RequiresQuestionAndAnswer, false, 256, "passwordAnswer");

				string name = GetUserNameFromMembershipUserName(username);
				string domain = GetDomainFromMembershipUserName(username);
				User user = this.UsersDAO.GetUserByUserName(name, domain);
				if(user == null) {
					throw new UserDoesNotExistException(string.Format("User with username {0} does not exist.", username));
				}
				if(this.RequiresQuestionAndAnswer && (user.Properties.PasswordAnswer == null || !user.Properties.PasswordAnswer.Equals(passwordAnswer, StringComparison.CurrentCultureIgnoreCase))) {
					throw new MembershipPasswordException();
				}
				return user.Properties[this.UsersDAO.PropertyNames.Password] != null && user.Properties[this.UsersDAO.PropertyNames.Password].Value != null ? user.Properties[this.UsersDAO.PropertyNames.Password].Value.ToString() : string.Empty;
			}
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets a collection of all the users in the data source in pages of data.
		/// </summary>
		/// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
		/// <param name="pageSize">The size of the page of results to return.</param>
		/// <param name="totalRecords">The total number of matched users.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
		/// </returns>
		public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords) {
			UserCollection uc = this.UsersDAO.GetAllUsers(pageIndex, pageSize, out totalRecords);
			MembershipUserCollection users = new MembershipUserCollection();
			foreach(User user in uc) {
				users.Add(new UsersDAOMembershipUser(user, this.Name));
			}
			return users;
		}

		/// <summary>
		/// Gets a collection of membership users where the user name contains the specified user name to match.
		/// </summary>
		/// <param name="usernameToMatch">The user name to search for.</param>
		/// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
		/// <param name="pageSize">The size of the page of results to return.</param>
		/// <param name="totalRecords">The total number of matched users.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
		/// </returns>
		public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords) {
			if(usernameToMatch == null)
				throw new ArgumentNullException("usernameToMatch");
			UsersDAOMembershipProvider.CheckParameter(usernameToMatch, true, true, false, 256, "usernameToMatch");
			usernameToMatch = usernameToMatch.Replace("%", string.Empty);
			MembershipUserCollection users = new MembershipUserCollection();
			string name = GetUserNameFromMembershipUserName(usernameToMatch);
			string domain = GetDomainFromMembershipUserName(usernameToMatch);
			User user = this.UsersDAO.GetUserByUserName(name, domain);
			totalRecords = 0;
			if(user != null) {
				users.Add(new UsersDAOMembershipUser(user, this.Name));
				totalRecords = 1;
			}
			return users;
		}

		/// <summary>
		/// Gets a collection of membership users where the e-mail address contains the specified e-mail address to match.
		/// </summary>
		/// <param name="emailToMatch">The e-mail address to search for.</param>
		/// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
		/// <param name="pageSize">The size of the page of results to return.</param>
		/// <param name="totalRecords">The total number of matched users.</param>
		/// <returns>
		/// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
		/// </returns>
		public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords) {
			if(emailToMatch == null)
				throw new ArgumentNullException("emailToMatch");
			UsersDAOMembershipProvider.CheckParameter(emailToMatch, false, false, false, 256, "emailToMatch");
			emailToMatch = emailToMatch.Replace("%", string.Empty);
			SearchCriteria sc = new SearchCriteria(this.UsersDAO.PropertyNames.Email, emailToMatch, SearchCriteria.CompareCondition.Contains);
			UserCollection uc = this.UsersDAO.GetUsersBySearchCriteria(sc);
			totalRecords = uc.Count;
			MembershipUserCollection users = new MembershipUserCollection();
			for(int i = pageIndex * pageSize; (i < ((pageIndex * pageSize) + pageSize)) && (i < uc.Count); i++) {
				users.Add(new UsersDAOMembershipUser(uc[i], this.Name));
			}
			return users;
		}
		#endregion
	}
}
