/*                       ____               ____________
 *                      |    |             |            |
 *                      |    |             |    ________|
 *                      |    |             |   |
 *                      |    |             |   |    
 *                      |    |             |   |    ____
 *                      |    |             |   |   |    |
 *                      |    |_______      |   |___|    |
 *                      |            |  _  |            |
 *                      |____________| |_| |____________|
 *                        
 *      Author(s):      limpygnome (Marcus Craske)              limpygnome@gmail.com
 * 
 *      License:        Creative Commons Attribution-ShareAlike 3.0 Unported
 *                      http://creativecommons.org/licenses/by-sa/3.0/
 * 
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/AccountActions.cs
 * 
 *      Change-Log:
 *                      2013-08-05      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A class of methods for applying actions to accounts, using a share of models.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using CMS.Base;
using CMS.BasicSiteAuth.Models;

namespace CMS.BasicSiteAuth
{
    public static class AccountActions
    {
        // Enums *******************************************************************************************************
        /// <summary>
        /// Used for the e-mail recovery method as the status of the operation.
        /// </summary>
        public enum RecoveryCodeEmail
        {
            /// <summary>
            /// Indicates the recovery-code exists.
            /// </summary>
            Exists,
            /// <summary>
            /// The recovery code does not exist or an error occurred.
            /// </summary>
            Failed,
            /// <summary>
            /// The new password could not be applied; check the status reference output of the method using this enum.
            /// </summary>
            FailedUserPersist,
            /// <summary>
            /// The user has attempted too many times and has been banned.
            /// </summary>
            FailedBanned,
            /// <summary>
            /// Successfully changed password and removed recovery code.
            /// </summary>
            Success,
        }
        /// <summary>
        /// Used for the secret question/answer method as the status of the operation.
        /// </summary>
        public enum RecoverySQA
        {
            /// <summary>
            /// Indicates the user exists.
            /// </summary>
            Exists,
            /// <summary>
            /// Indicates SQA is disabled for the user.
            /// </summary>
            FailedDisabled,
            /// <summary>
            /// Indicates the answer is incorrect.
            /// </summary>
            FailedAnswer,
            /// <summary>
            /// Indicates the user is banned from too many attempts.
            /// </summary>
            FailedBanned,
            /// <summary>
            /// Indicates a persistence issue occurred.
            /// </summary>
            FailedPersist,
            /// <summary>
            /// Indicates an error occurred during the operation.
            /// </summary>
            Failed,
            /// <summary>
            /// Indicates the answer was correct and the password has been changed.
            /// </summary>
            Success
        }
        public enum AccountUpdate
        {
            /// <summary>
            /// Indicates a general exception occurred.
            /// </summary>
            Failed,
            /// <summary>
            /// Indicates the current password is incorrect
            /// </summary>
            FailedCurrentPassword,
            /// <summary>
            /// Indicates the password could not be updated.
            /// </summary>
            FailedPassword,
            /// <summary>
            /// Indicates the user settings failed to persist.
            /// </summary>
            FailedUserPersist,
            /// <summary>
            /// Indicates the operation was successful.
            /// </summary>
            Success
        }
        // Methods - Static - Registration *****************************************************************************
        /// <summary>
        /// Used for handling an account verification, upgrading the group from unverified to verified.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">The BSA plugin.</param>
        /// <param name="code">The code of the recovery code.</param>
        /// <param name="email">The e-mail of the account which owns the code, an additional layer of security against brute-force.</param>
        /// <returns>True if successfully verified, false if not found.</returns>
        public static bool accountVerify(Data data, BasicSiteAuth bsa, string code, string email)
        {
            // Check the IP is not banned (brute-force protection)
            if (AuthFailedAttempt.isIpBanned(data.Connector, data.Request.UserHostAddress))
                return false;
            // Lookup the code
            AccountCode c = AccountCode.load(code, email, AccountCode.CodeType.Recovery, data.Connector);
            if (c != null)
            {
                User usr = User.load(bsa, data.Connector, c.UserID);
                if (usr != null && usr.UserGroup.GroupID == Core.Settings[BasicSiteAuth.SETTINGS_GROUP_UNVERIFIED_GROUPID].get<int>())
                {
                    // Switch the user-group
                    usr.UserGroup = bsa.UserGroups[Core.Settings[BasicSiteAuth.SETTINGS_GROUP_USER_GROUPID].get<int>()];
                    if (usr.save(bsa, data.Connector) != User.UserCreateSaveStatus.Success)
                        return false;
                }
                // Remove the code
                c.remove(data.Connector);
                return true;
            }
            else
                // Log as an attempt to authenticate - brute-force protection
                AuthFailedAttempt.create(data, AuthFailedAttempt.AuthType.Recovery);
            return false;
        }
        // Methods - Static - Recovery *********************************************************************************
        /// <summary>
        /// Attempts to deploy a recovery-code to a user
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="email">The e-mail of the account.</param>
        /// <returns>Either Failed, FailedBanned or Success.</returns>
        public static RecoveryCodeEmail recoveryEmailDeploy(Data data, BasicSiteAuth bsa, string email)
        {
            // Check the user is not banned
            if (AuthFailedAttempt.isIpBanned(data.Connector, data.Request.UserHostAddress))
                return RecoveryCodeEmail.FailedBanned;
            // Lookup the user by e-mail
            User u = User.loadByEmail(bsa, data.Connector, email);
            if (u != null)
            {
                // Create a new recovery code
                AccountCode rc = AccountCode.create(data.Connector, AccountCode.CodeType.Recovery, u);
                // E-mail the user
                Emails.sendRecoveryCode(data, u, rc.Code);
                // Log the event
                AccountEvent.create(data.Connector, bsa, BasicSiteAuth.ACCOUNT_EVENT__RECOVERYCODE_SENT__UUID, DateTime.Now, u.UserID, data.Request.UserHostAddress, SettingsNode.DataType.String, data.Request.UserAgent, SettingsNode.DataType.String);
                return rc == null ? RecoveryCodeEmail.Failed : RecoveryCodeEmail.Success;
            }
            else
                AuthFailedAttempt.create(data, AuthFailedAttempt.AuthType.Recovery);
            return RecoveryCodeEmail.Failed;
        }
        /// <summary>
        /// Used for handling a recovery-code used for account recovery/password-reset by e-mail. The newPassword
        /// parameter can be left null or empty to indicate if a recovery code exists.
        /// 
        /// If FailedUserPersist occurs, the status of why the persistence failed will be outputted to the parameter
        /// persistStatus.
        /// 
        /// If the code does not exist, Failed is returned.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">The BSA plugin.</param>
        /// <param name="code">The code of the recovery-code.</param>
        /// <param name="newPassword">The new password; can be null or empty.</param>
        /// <param name="persistStatus">This parameter is set with the status of persisting the new password.</param>
        /// <returns>The status of the operation.</returns>
        public static RecoveryCodeEmail recoveryEmail(Data data, BasicSiteAuth bsa, string code, string newPassword, ref User.UserCreateSaveStatus persistStatus)
        {
            // Check the IP is not banned (brute-force protection)
            if (AuthFailedAttempt.isIpBanned(data.Connector, data.Request.UserHostAddress))
                return RecoveryCodeEmail.FailedBanned;
            // Lookup the code
            AccountCode c = AccountCode.load(code, AccountCode.CodeType.Recovery, data.Connector);
            if (c != null)
            {
                // Check if to just confirm the code exists
                if (newPassword == null || newPassword.Length == 0)
                    return RecoveryCodeEmail.Exists;
                // Change the user's password
                User usr = User.load(bsa, data.Connector, c.UserID);
                User.UserCreateSaveStatus s = usr.setPassword(bsa, newPassword);
                if (s != User.UserCreateSaveStatus.Success)
                {
                    persistStatus = s;
                    return RecoveryCodeEmail.FailedUserPersist;
                }
                // Persist the change
                s = usr.save(bsa, data.Connector);
                if (s != User.UserCreateSaveStatus.Success)
                {
                    persistStatus = s;
                    return RecoveryCodeEmail.FailedUserPersist;
                }
                // Remove all codes owned by the user (no need for them any longer)
                AccountCode.remove(data.Connector, usr);
                c.remove(data.Connector);
                // Log the event
                AccountEvent.create(data.Connector, bsa, BasicSiteAuth.ACCOUNT_EVENT__CHANGEDSETTINGS__UUID, DateTime.Now, usr.UserID, data.Request.UserHostAddress, SettingsNode.DataType.String, data.Request.UserAgent, SettingsNode.DataType.String);
                return RecoveryCodeEmail.Success;
            }
            else
                // Log as an attempt to authenticate - brute-force protection
                AuthFailedAttempt.create(data, AuthFailedAttempt.AuthType.Recovery);
            return RecoveryCodeEmail.Failed;
        }
        /// <summary>
        /// Used for handling a secret question/answer recovery process.
        /// 
        /// The parameter secretAnswer can be left null or empty to indicate if an account exists.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">The BSA plugin.</param>
        /// <param name="username">The username of the user.</param>
        /// <param name="secretAnswer">The secret answer; can be left null or empty.</param>
        /// <param name="newPassword">The new password for the user; can also be left null or empty if the secret answer is also null or empty..</param>
        /// <param name="secretQuestion">The user's secret question is outputted to this parameter.</param>
        /// <param name="persistStatus">When the returned value is FailedPersist, the reason for failure is outputted to this parameter.</param>
        public static RecoverySQA recoverySQA(Data data, BasicSiteAuth bsa, string username, string secretAnswer, string newPassword, ref string secretQuestion, ref User.UserCreateSaveStatus persistStatus)
        {
            if (username == null)
                return RecoverySQA.Failed;
            // Fetch the user
            User u = User.load(bsa, data.Connector, username);
            if (u != null)
            {
                // Check if SQA is disabled
                if (u.SecretQuestion == null || u.SecretAnswer == null || u.SecretQuestion.Length == 0 || u.SecretAnswer.Length == 0)
                    return RecoverySQA.FailedDisabled;
                // Output question
                secretQuestion = u.SecretQuestion;
                // Check if to just indicate the user exists
                if (secretAnswer == null || secretAnswer.Length == 0)
                    return RecoverySQA.Exists;
                // Check the answer matches the actual answer
                if (u.SecretAnswer.ToLower() == secretAnswer.ToLower())
                {
                    // Attempt to change the password
                    User.UserCreateSaveStatus s = u.setPassword(bsa, newPassword);
                    if (s != User.UserCreateSaveStatus.Success || (s = u.save(bsa, data.Connector)) != User.UserCreateSaveStatus.Success)
                    {
                        persistStatus = s;
                        return RecoverySQA.FailedPersist;
                    }
                    else
                    { // Success
                        return RecoverySQA.Success;
                    }
                }
                else
                {
                    // Log the attempt
                    AccountEvent.create(data.Connector, bsa, BasicSiteAuth.ACCOUNT_EVENT__SECRETQA_ATTEMPT__UUID, DateTime.Now, u.UserID, data.Request.UserHostAddress, SettingsNode.DataType.String, data.Request.UserAgent, SettingsNode.DataType.String);
                    AuthFailedAttempt.create(data, AuthFailedAttempt.AuthType.Recovery);
                }
            }
            return RecoverySQA.Failed;
        }
        // Methods - Static - Account **********************************************************************************
        /// <summary>
        /// Updates the settings of the current user's account.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="user">The current user of the session.</param>
        /// <param name="currentPassword">The user's current password.</param>
        /// <param name="password">The new password; can be left null or empty.</param>
        /// <param name="secretQuestion">The new secret question; can be left null.</param>
        /// <param name="secretAnswer">The new secret answer; can be left null.</param>
        /// <param name="persistStatus">If the returned status of this operation is FailedUserPersist or FailedCurrentPassword, the persistence issue is outputted to this parameter.</param>
        /// <returns>The status of the operation.</returns>
        public static AccountUpdate updateAccount(Data data, BasicSiteAuth bsa, User user, string currentPassword, string password, string secretQuestion, string secretAnswer, ref User.UserCreateSaveStatus persistStatus)
        {
            // Check the required parameters are valid
            if (data == null || user == null)
                return AccountUpdate.Failed;
            else if (currentPassword == null)
                return AccountUpdate.FailedCurrentPassword;
            // Check the current password is correct
            UserBan ban = null;
            User.AuthenticationStatus s = user.authenticate(bsa, currentPassword, data, ref ban);
            if (s == User.AuthenticationStatus.Failed)
                return AccountUpdate.Failed;
            else if (s == User.AuthenticationStatus.FailedBanned || s == User.AuthenticationStatus.FailedDisabled || s == User.AuthenticationStatus.FailedIncorrect || s == User.AuthenticationStatus.FailedTempBanned)
                return AccountUpdate.FailedCurrentPassword;
            else
            {
                User.UserCreateSaveStatus up;
                // Update the model
                // -- Password
                if (password != null && password.Length > 0 && (up = user.setPassword(bsa, password)) != User.UserCreateSaveStatus.Success)
                {
                    persistStatus = up;
                    return AccountUpdate.FailedPassword;
                }
                // -- Secret question
                if (secretQuestion != null)
                    user.SecretQuestion = secretQuestion;
                // -- Secret answer
                if (secretAnswer != null)
                    user.SecretAnswer = secretAnswer;
                // Save the model
                up = user.save(bsa, data.Connector);
                if (up != User.UserCreateSaveStatus.Success)
                {
                    persistStatus = up;
                    return AccountUpdate.FailedUserPersist;
                }
                else
                    return AccountUpdate.Success;
            }
        }
    }
}