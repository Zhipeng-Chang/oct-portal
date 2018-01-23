﻿using CoE.Ideas.Core;
using CoE.Ideas.Core.ServiceBus;
using CoE.Ideas.Core.WordPress;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoE.Ideas.Integration.Notification
{
    public class IdeaLoggedListener : IdeaListener
    {
        public IdeaLoggedListener(IIdeaRepository ideaRepository,
            IWordPressClient wordPressClient, 
            IMailmanEnabledSheetReader mailmanSheetReader,
            IEmailService emailService,
            string mergeTemplateName)
        : base(ideaRepository, wordPressClient)
        {
            _mailmanSheetReader = mailmanSheetReader;
            _emailService = emailService;
            _mergeTemplateName = mergeTemplateName;
        }

        private readonly IMailmanEnabledSheetReader _mailmanSheetReader;
        private readonly IEmailService _emailService;
        private readonly string _mergeTemplateName;
        private Func<object> getRequiredService;

        protected override bool ShouldProcessMessage(IdeaMessage message)
        {
            return message.Type == IdeaMessageType.IdeaLogged;
        }


        public override async Task<MessageProcessResponse> OnMessageRecevied(IdeaMessage message, IDictionary<string, object> properties)
        {
            try
            {
                if (message == null)
                    throw new ArgumentNullException("message");

                if (ShouldProcessMessage(message))
                {
                    var mergeTemplate = await _mailmanSheetReader.GetMergeTemplateAsync(_mergeTemplateName);
                    if (mergeTemplate != null)
                    {
                        string ideaRange = properties.ContainsKey("RangeUpdated") ? properties["RangeUpdated"] as string : null;

                        IDictionary<string, object> ideaData;
                        if (string.IsNullOrWhiteSpace(ideaRange))
                        {
                            ideaData = await _mailmanSheetReader.GetValuesAsync(mergeTemplate, message.IdeaId);
                        }
                        else
                        {
                            ideaData = await _mailmanSheetReader.GetValuesAsync(mergeTemplate, ideaRange);
                        }

                        if (ideaData != null)
                        {
                            _emailService.SendEmailAsync(mergeTemplate, ideaData);
                        }
                    }
                }

                return MessageProcessResponse.Complete;
            }
            catch (Exception err)
            {
                // log the error
                System.Diagnostics.Trace.TraceError($"Error processing idea message: { err.Message }");

                // abandon message?
                return MessageProcessResponse.Abandon;
            }
        }

        protected override Task ProcessIdeaMessage(IdeaMessage message, Idea idea, WordPressUser wordPressUser)
        {
            // TODO: Refactor this as it is never called
            throw new NotImplementedException();
        }
    }
}