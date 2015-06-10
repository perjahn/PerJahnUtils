using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace mailer
{
    class Simple
    {
        static void Main2(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine(@"Usage: mailer <to> <from> <subject> <message> <smtpserver>");
                Console.WriteLine(@"Example: mailer per.jahn@domain.se noreply@somewhere.se ""Hello!"" ""bla bla bla"" mail.domain.com");
                return;
            }

            SmtpClient smtpClient = new SmtpClient(args[4]);
            MailMessage message = new MailMessage(args[1], args[0], args[2], args[3].Replace(@"\n", "\n"));
            smtpClient.Send(message);
            System.Threading.Thread.Sleep(5000);
        }
    }
}
