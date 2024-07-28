using System;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace mailer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length is < 5 or > 8)
                {
                    Console.WriteLine(@"Usage: mailer <to> <from> <subject> <message> <smtpserver> [user] [pass] [attachfile]");
                    Console.WriteLine(@"Example: mailer per.jahn@domain.se noreply@somewhere.se ""Hello!"" ""bla bla bla"" mail.domain.com");
                    return;
                }

                var to = args[0];
                var from = args[1];
                var subject = args[2];
                var body = args[3].Replace(@"\n", "\n");
                var smtpServer = args[4];
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

                SmtpClient smtpClient;
                var port = -1;
                var separator = smtpServer.IndexOf(':');
                if (separator >= 0)
                {
                    var p = smtpServer[(separator + 1)..];
                    if (!int.TryParse(p, out port) || port < 0)
                    {
                        Console.WriteLine($"Invalid port: '{p}'");
                        return;
                    }
                    smtpServer = smtpServer[..separator];
                }

                using var smtpClient = port < 0 ? new SmtpClient(smtpServer) : new SmtpClient(smtpServer, port);

                if (username != null && password != null)
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                }

                using MailMessage message = new(from, to, subject, body);
                smtpClient.EnableSsl = true;

                if (filename != null)
                {
                    Attachment att = new(filename);
                    message.Attachments.Add(att);
                }

                smtpClient.Send(message);

                // Sleep a little while, required for sending email to some smtp servers.
                Thread.Sleep(5000);

                Console.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
