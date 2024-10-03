using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AIForged;
using AIForged.API;

using AIForgedPollingExample.Clients;

namespace AIForgedPollingExample.WorkerFunctions
{
    public partial class AIForgedWorker
    {
        private ILogger _logger;
        private Models.IConfig _config;
        private AIForgedClient _aiforgedClient;

        /// <summary>
        /// Creates a new instance of the AIForgedWorker with the app logger and config
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public AIForgedWorker(ILogger logger, Models.IConfig config)
        {
            _logger = logger;
            _config = config;

            //Create a new instance of our AIForgedClient using our logger and config
            _aiforgedClient = new AIForgedClient(logger, config);
        }

        /// <summary>
        /// Poll AIForged for documents that match a specific criteria (as configured in the app).
        /// </summary>
        /// <returns>Returns a boolean value indicating success or failure</returns>
        public async Task<bool> PollAndStoreDataAsync()
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("AIForgedWorker - PollAndStoreDataAsync: {time} - Retrieving documents...", DateTimeOffset.Now);
                }
                //Retrieve our documents based on our app config
                ICollection<DocumentViewModel> docs = await _aiforgedClient.GetDocumentsAsync(DateTime.UtcNow - _config.StartDateTimeSpan, DateTime.UtcNow, DocumentStatus.CustomVerified);

                //Stop processing if there was an exception while retrieving docs
                if (docs is null)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogInformation("AIForgedWorker - PollAndStoreDataAsync: {time} - Document retrieval exception. Returning...", DateTimeOffset.Now);
                    }
                    return false;
                }
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("AIForgedWorker - PollAndStoreDataAsync: {time} - Found {count} documents.", DateTimeOffset.Now, docs.Count);
                }

                //Iterate through our returned docs
                foreach (DocumentViewModel doc in docs)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("AIForgedWorker - PollAndStoreDataAsync: {time} - Start processing document: {doc}", DateTimeOffset.Now, doc);
                    }
                    //Get document extraction results in a "Flat File" structure
                    var docResults = await _aiforgedClient.GetDocumentDataAsync(doc);

                    //Get document extraction results in a traversible hierarchy structure
                    var docResultsHierarchy = await _aiforgedClient.GetDocumentDataHierarchyAsync(doc);

                    //Get the first Document Parameter result matching a given Parameter Definition Id
                    var result = _aiforgedClient.GetResultRecursive(docResultsHierarchy, 212751);

                    //Get all Document Parameter results matching a given Parameter Definition Id
                    var results = _aiforgedClient.GetResultsRecursive(docResultsHierarchy, 212751);

                    //Update our document status server side to indicate processing is complete
                    await _aiforgedClient.UpdateDocumentStatusAsync(doc, DocumentStatus.CustomProcessed, "Hello from Skills Sharing Session 1");

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("AIForgedWorker - PollAndStoreDataAsync: {time} - Done processing document: {doc}", DateTimeOffset.Now, doc);
                    }
                }

                //Indicate success
                return true;
            }
            catch (Exception ex)
            {
            }
            //Indicate failure
            return false;
        }
    }
}
