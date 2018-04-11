﻿using NeoSharp.Core;

namespace NeoSharp.Application.Client
{
    public class ClientManager : IBootstrapper
    {
        #region Variables

        /// <summary>
        /// Prompt
        /// </summary>
        private readonly IPrompt _prompt;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="promptInit">Prompt</param>
        public ClientManager(IPrompt promptInit)
        {
            this._prompt = promptInit;
        }

        /// <summary>
        /// Run client with arguments
        /// </summary>
        /// <param name="args">Arguments</param>
        public void Start(string[] args)
        {            
            this._prompt.StartPrompt(args);
        }
    }
}