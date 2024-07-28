﻿using System;
using System.Net.Mail;
using System.Threading;

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

            using SmtpClient smtpClient = new(args[4]);
            using MailMessage message = new(args[1], args[0], args[2], args[3].Replace(@"\n", "\n"));
            smtpClient.Send(message);
            Thread.Sleep(5000);
        }
    }
}
