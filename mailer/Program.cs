using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace mailer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 5 || args.Length > 8)
                {
                    Console.WriteLine(@"Usage: mailer <to> <from> <subject> <message> <smtpserver> [user] [pass] [attachfile]");
                    Console.WriteLine(@"Example: mailer per.jahn@domain.se noreply@somewhere.se ""Hello!"" ""bla bla bla"" mail.domain.com");
                    return;
                }

                string to = args[0];
                string from = args[1];
                string subject = args[2];
                string body = args[3].Replace(@"\n", "\n");
                string smtpServer = args[4];
                string username = null;
                string password = null;
                string filename = null;


                if (args.Length == 6)
                {
                    filename = args[5];
                }
                if (args.Length == 7)
                {
                    username = args[5];
                    password = args[6];
                }
                if (args.Length == 8)
                {
                    username = args[5];
                    password = args[6];
                    filename = args[7];
                }


                Console.WriteLine($"Using: to: '{to}' from: '{from}' subject: '{subject}' body: '{body}' smtpserver: '{smtpServer}' filename: '{filename}'");

                SmtpClient smtpClient = new SmtpClient(smtpServer);

                if (username != null && password != null)
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(username, password);
                }

                MailMessage message = new MailMessage(from, to, subject, body);

                if (filename != null)
                {
                    Attachment att = new Attachment(filename);
                    message.Attachments.Add(att);
                }

                smtpClient.Send(message);

                // Sleep a little while, required for sending email to some smtp servers.
                System.Threading.Thread.Sleep(5000);

                Console.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
