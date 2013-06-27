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
 *      File:           EmailQueue.cs
 *      Path:           /App_Code/CMS/Core/EmailQueue.cs
 * 
 *      Change-Log:
 *                      2013-06-25     Created initial class.
 * 
 * *****************************************************************************
 * An email-queue service for mass-sending e-mails in a seperate thread. This
 * system also saves the buffer of e-mails, to be sent, in the database in-case
 * the web application is interrupted (failure, shutdown, etc). The general idea
 * is to ensure any e-mails deployed by a plugin are delivered to the mail-server.
 * *****************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using UberLib.Connector;

namespace CMS
{
	namespace Base
	{
		public class EmailQueue
		{
			// Fields **************************************************************************************************
			private Thread cyclerThread;
			// Fields - Settings ***************************************************************************************
			bool	enabled;			// Indicates if settings have been specified for the e-mail queue service to run.
			string 	mailHost,			// The mail-server host.
					mailUsername,		// The mail-server username for authentication.
					mailPassword,		// The mail-server password for authentication.
					mailAddress;		// The e-mail address used in e-mails sent by the mail-server.
			int		mailPort,			// The mail-server port.
					errors;				// The number of errors occurred whilst sending e-mails.
			// Methods - Constructors **********************************************************************************
			private EmailQueue()
			{
				cyclerThread = null;
				errors = 0;
				enabled = false;
			}
			// Methods *************************************************************************************************
			/// <summary>
			/// Adds a new e-mail message to the queue.
			/// </summary>
			/// <param name="conn">Database connector.</param>
			/// <param name="destinationEmail">Destination email.</param>
			/// <param name="subject">Subject.</param>
			/// <param name="body">Body.</param>
			/// <param name="html">Indicates if the body is HTML (true) or not (false).</param>
			public void addMessage(Connector conn, string destinationEmail, string subject, string body, bool html)
			{
				conn.Query_Execute("INSERT INTO cms_email_queue (email, subject, body, html) VALUES('" + Utils.Escape(destinationEmail) + "', '" + Utils.Escape(subject) + "', '" + Utils.Escape(body) + "', '" + (html ? "1" : "0") + "');");
			}
			// Methods - Threading and Cycling *************************************************************************
			private void cycler()
			{
				// Setup the client for deploying e-mails
				SmtpClient client = new SmtpClient();
				client.Host = mailHost;
				client.Port = mailPort;
				client.Credentials = new NetworkCredential(mailUsername, mailPassword);
				// Prepare the query for polling the database
				int messageThroughPut = Core.SettingsDisk["settings/mail/message_throughput"].Length > 0 ? int.Parse(Core.SettingsDisk["settings/mail/message_throughput"]) : 5;
				int messagePollDelay = Core.SettingsDisk["settings/mail/message_poll_delay"].Length > 0 ? int.Parse(Core.SettingsDisk["settings/mail/message_poll_delay"]) : 100;
				string queryPollMessages = "SELECT email, subject, body, html FROM cms_email_queue ORDER BY emailid ASC LIMIT " + messageThroughPut;
				// Poll for messages
				Result msgs;
				MailMessage compiledMessage;
				StringBuilder queryUpdate;
				while(true)
				{
					try
					{
						// Fetch the next message
						msgs = Core.Connector.Query_Read(queryPollMessages);
						// Send each message
						queryUpdate = new StringBuilder();
						foreach(ResultRow msg in msgs)
						{
							try
							{
								compiledMessage = new MailMessage();
								compiledMessage.To.Add(msg["email"]);
								compiledMessage.From = new MailAddress(mailAddress);
								compiledMessage.Subject = msg["subject"];
								compiledMessage.Headers.Add("CMS", "Uber CMS");
								compiledMessage.Body = msg["body"];
								compiledMessage.IsBodyHtml = msg["html"].Equals("1");
								client.Send(compiledMessage);
								// Append query to update the database
								queryUpdate.Append("DELETE FROM cms_email_queue WHERE emailid='" + Utils.Escape(msg["emailid"]) + "';");
							}
							catch(SmtpException)
							{
								if(errors < int.MaxValue - 1)
									errors++;
								else
									errors = 1;
							}
						}
						// Update the database
						Core.Connector.Query_Execute(queryUpdate.ToString());
					}
					catch {}
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
					if(cyclerThread != null || !enabled)
						return;
					cyclerThread = new Thread(
						delegate()
						{
							cycler();
						}
					);
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
				}
			}
			// Methods - Static ****************************************************************************************
			/// <summary>
			/// Creates a new instance of a configured and operational e-mail queue.
			/// </summary>
			public static EmailQueue create()
			{
				EmailQueue queue = new EmailQueue();
				// Load configuration
				queue.mailHost = Core.Settings["settings/mail/host"];
				queue.mailPort = int.Parse(Core.Settings["settings/mail/port"]);
				queue.mailUsername = Core.Settings["settings/mail/username"];
				queue.mailPassword = Core.Settings["settings/mail/password"];
				queue.mailAddress = Core.Settings["settings/mail/email"];
				if(queue.mailHost.Length != 0 && queue.mailUsername.Length != 0 && queue.mailAddress.Length != 0)
				{
					queue.enabled = true;
					// Start cycler
					queue.start();
				}
				return queue;
			}
		}
	}
}

