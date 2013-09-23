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
 *      Path:           /App_Code/CMS/Base/EmailQueue.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * An email-queue service for mass-sending e-mails in a seperate thread. This system also saves the buffer of e-mails,
 * to be sent, in the database in-case the web application is interrupted (failure, shutdown, etc). The general idea
 * is to ensure any e-mails deployed by a plugin are delivered to the mail-server.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using UberLib.Connector;

namespace CMS.Base
{
    /// <summary>
    /// An email-queue service for mass-sending e-mails in a seperate thread. This system also saves the buffer of
    /// e-mails, to be sent, in the database in-case the web application is interrupted (failure, shutdown, etc). The
    /// general idea is to ensure any e-mails deployed by a plugin are delivered to the mail-server.
    /// </summary>
	public class EmailQueue
	{
		// Fields ******************************************************************************************************
		private Thread  cyclerThread;
		// Fields - Settings *******************************************************************************************
		private bool	enabled;			// Indicates if settings have been specified for the e-mail queue service to run.
		private string 	mailHost,			// The mail-server host.
				        mailUsername,		// The mail-server username for authentication.
				        mailPassword,		// The mail-server password for authentication.
				        mailAddress;		// The e-mail address used in e-mails sent by the mail-server.
		private int		mailPort,			// The mail-server port.
				        errors;				// The number of errors occurred whilst sending e-mails.
		// Methods - Constructors **************************************************************************************
		private EmailQueue()
		{
			this.cyclerThread = null;
            this.errors = 0;
            this.enabled = false;
		}
		// Methods - Threading and Cycling *****************************************************************************
		private void cycler()
		{
			// Setup the client for deploying e-mails
			SmtpClient client = new SmtpClient();
			client.Host = mailHost;
			client.Port = mailPort;
			client.Credentials = new NetworkCredential(mailUsername, mailPassword);
			// Prepare the query for polling the database
			int messageThroughPut = Core.SettingsDisk["settings/mail/message_throughput"].get<int>();
			int messagePollDelay = Core.SettingsDisk["settings/mail/message_poll_delay"].get<int>();
            int messageErrorThreshold = Core.SettingsDisk["settings/mail/message_error_threshold"].get<int>();
            if (messageErrorThreshold < 1)
            {
                Core.fail("Invalid message error threshold of '" + messageErrorThreshold + "' - this would delete every single e-mail before being sent!");
                return;
            }
            if (messagePollDelay < 0 || messageThroughPut < 1)
            {
                Core.fail("Invalid e-mail queue settings with either message poll delay or message through-put - aborting!");
                // Protection in-case aborting the thread failed
                cyclerThread = null;
                return;
            }
            string queryPollMessages = "DELETE FROM cms_email_queue WHERE errors > '" + messageErrorThreshold.ToString() + "'; SELECT * FROM cms_view_email_queue LIMIT " + messageThroughPut + ";";
			// Poll for messages
            bool success;
			Result msgs;
			MailMessage compiledMessage;
			StringBuilder queryUpdate;
            Connector conn;
			while(true)
			{
                try
                {
                    // Setup the connector
                    conn = Core.connectorCreate(false);
                    // Fetch the next message
                    msgs = conn.queryRead(queryPollMessages);
                    // Send each message
                    queryUpdate = new StringBuilder();
                    foreach (ResultRow msg in msgs)
                    {
                        success = false;
                        try
                        {
                            compiledMessage = new MailMessage();
                            compiledMessage.To.Add(new MailAddress(msg["email"]));
                            compiledMessage.From = new MailAddress(mailAddress);
                            compiledMessage.Subject = msg["subject"];
                            compiledMessage.Headers.Add("CMS", "Uber CMS");
                            compiledMessage.Body = msg["body"];
                            compiledMessage.IsBodyHtml = msg["html"].Equals("1");
                            client.Send(compiledMessage);
                            // Append query to update the database
                            queryUpdate.Append("DELETE FROM cms_email_queue WHERE emailid='" + SQLUtils.escape(msg["emailid"]) + "';");
                            success = true;
                        }
                        catch (SmtpException)
                        {
                            if (errors < int.MaxValue - 1)
                                errors++;
                            else
                                errors = 1;
                        }
                        // Increment the error-count for the e-mail by one
                        if (!success)
                            queryUpdate.Append("UPDATE cms_email_queue SET errors = errors + 1, last_sent=CURRENT_TIMESTAMP WHERE emailid='" + SQLUtils.escape(msg["emailid"]) + "';");
                    }
                    // Update the database
                    if(queryUpdate.Length > 0)
                        conn.queryExecute(queryUpdate.ToString());
                    // Dispose the connector
                    conn.disconnect();
                    conn = null;
                }
                catch
                {
                    //Core.fail("E-mail queue exception: '" + ex.GetBaseException().Message + "' - '" + ex.StackTrace + "'!");
                    //return;
                }
				// Sleep to avoid excessive CPU usage
				Thread.Sleep(messagePollDelay);
			}
		}
		/// <summary>
		/// Starts the e-mail queue worker.
		/// </summary>
		public void start()
		{
			lock(this)
			{
				if(cyclerThread != null || enabled)
					return;
				cyclerThread = new Thread(
					delegate()
					{
						cycler();
					}
				);
                cyclerThread.Start();
                enabled = true;
			}
		}
		/// <summary>
		/// Stops the e-mail queue worker.
		/// </summary>
		public void stop()
		{
			lock(this)
			{
			    if(cyclerThread == null)
					    return;
				cyclerThread.Abort();
				cyclerThread = null;
                enabled = false;
			}
		}
		// Methods - Static ********************************************************************************************
		/// <summary>
		/// Creates a new instance of a configured and operational e-mail queue.
		/// </summary>
		public static EmailQueue create()
		{
			EmailQueue queue = new EmailQueue();
			// Load configuration
            queue.mailHost = Core.SettingsDisk["settings/mail/host"].get<string>();
			queue.mailPort = Core.SettingsDisk["settings/mail/port"].get<int>();
			queue.mailUsername = Core.SettingsDisk["settings/mail/user"].get<string>();
			queue.mailPassword = Core.SettingsDisk["settings/mail/pass"].get<string>();
			queue.mailAddress = Core.SettingsDisk["settings/mail/email"].get<string>();
            return queue;
		}
	}
}

