using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AIForged;
using AIForged.API;

namespace AIForgedPollingExample.Clients
{
    public partial class AIForgedClient
    {
        private ILogger _logger;
        private Models.IConfig _config;

        public AIForged.API.Context Context { get; private set; }

        /// <summary>
        /// Creates a new instance of the AIForgedClient with the app logger and config and intializes AIForged Context.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public AIForgedClient(ILogger logger, AIForgedPollingExample.Models.IConfig config) 
        {
            _logger = logger;
            _config = config;

            Init();
        }

        //Initializes the AIForged Config using the application configuration
        private void Init()
        {
            AIForged.API.Config config = new AIForged.API.Config(_config.EndPoint, "Skills Sharing Session 1");

            //This initializes the internal HttpClient, etc.
            config.Init();

            //Set the authentication type based on the application configuration
            if (!string.IsNullOrEmpty(_config.ApiKey)) 
            { 
                //Use API Key
                config.Auth = new System.Net.Http.Headers.AuthenticationHeaderValue("X-Api-Key", _config.ApiKey);
                config.HttpClient.DefaultRequestHeaders.Add("X-Api-Key", _config.ApiKey);
            }
            else
            {
                //Use OAuth Session
                config.UserName = _config.Username;
                config.Password = _config.Password;
            }

            //Create an instance of the AIForged Context from our config
            Context = new Context(config);
        }

        /// <summary>
        /// Test user / API Key authentication.
        /// </summary>
        /// <returns>Returns a boolean value indicating success.</returns>
        public async Task<bool> AuthorizeAsync()
        {
            try
            {
                await Context.GetCurrentUserAsync();
                //Indicate success
                return true;
            }
            catch (SwaggerException swex)
            {
                //Todo: Handle with authentication failures
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogInformation("AIForgedClient - AuthorizeAsync: {time} - Authorization failed: {swex}", DateTimeOffset.Now, swex);
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogInformation("AIForgedClient - AuthorizeAsync: {time} - Unknown exception occurred: {swex}", DateTimeOffset.Now, ex);
                }
            }
            //Indicate failure
            return false;
        }

        /// <summary>
        /// Returns a list of documents based on filtering criteria for the project and service Id combination in the application configuration.
        /// </summary>
        /// <param name="startDate">The start date to filter from.</param>
        /// <param name="endDate">The end date to filter to.</param>
        /// <param name="status">An optional Document Status to filter on.</param>
        /// <param name="usage">An optional Document UsageType to filter on.</param>
        /// <param name="masterId">An optional Document Id to filter children documents on.</param>
        /// <param name="documentId">An optional Document Id to filter on.</param>
        /// <returns></returns>
        public async Task<ICollection<AIForged.API.DocumentViewModel>> GetDocumentsAsync(DateTime? startDate, 
            DateTime? endDate, 
            DocumentStatus? status = DocumentStatus.Processed, 
            UsageType? usage = UsageType.Inbox,
            int? masterId = null,
            int? documentId = null)
        {
            try
            {
                //First we ensure that we are authorized to make an API call.
                bool isAuthorised = await AuthorizeAsync();
                
                //Stop processing if we are not authorized.
                if (!isAuthorised) return null;

                //Retrieve a list of documents based on the provided application configuration and filter criteria.
                var docResults = await Context.DocumentClient.GetExtendedAsync(userId: Context.CurrentUserId,
                    projectId: _config.ProjectId,
                    stpdId: _config.ServiceId,
                    usage: usage,
                    statuses: (status != null ? [status.Value] : null),
                    classname: null,
                    filename: null,
                    filetype: null,
                    start: startDate,
                    end: endDate,
                    masterid: masterId,
                    includeparamdefcategories: null,
                    pageNo: null,
                    pageSize: null,
                    sortField: SortField.Id,
                    sortDirection: SortDirection.Descending,
                    comment: null,
                    result: null,
                    resultId: null,
                    resultIndex: null,
                    externalId: null,
                    docGuid: null,
                    classId: null,
                    id: documentId);

                //Ensure the API call was successful before we return data
                if (docResults.StatusCode >= 200 &&  docResults.StatusCode < 300)
                {
                    //Return the collection of documents
                    return docResults.Result;
                }
            }
            catch (SwaggerException swex)
            {
                //Handle any API exceptions.
                if (swex.StatusCode == 404)
                {
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogInformation("AIForgedClient - GetDocumentsAsync: {time} - API call failed: {swex}", DateTimeOffset.Now, swex);
                }
            }
            //Return null if an exception was raised
            return null;
        }

        /// <summary>
        /// Returns a given document's extraction results in a "Flat File" structure.
        /// </summary>
        /// <param name="doc">The document to retrieve the extraction results for.</param>
        /// <returns>Returns a collection of DocumentParameterViewModel objects</returns>
        public async Task<ICollection<AIForged.API.DocumentParameterViewModel>> GetDocumentDataAsync(DocumentViewModel doc)
        {
            try
            {
                //First we ensure that we are authorized to make an API call.
                bool isAuthorised = await AuthorizeAsync();

                //Stop processing if we are not authorized.
                if (!isAuthorised) return null;
                
                //Retrieve the latest result documents for given document
                var latestChildDocs = await GetDocumentsAsync(startDate: null,
                    endDate: null,
                    status: null,
                    usage: UsageType.Outbox,
                    masterId: doc.Id,
                    documentId: null);

                //Retrieve the latest result document's extraction results
                var docResults = await Context.ParametersClient.GetAsync(docId: latestChildDocs.FirstOrDefault()?.Id,
                    stpdId: _config.ServiceId,
                    category: ParameterDefinitionCategory.Results,
                    grouping: null,
                    includeverification: true);

                //Ensure the API call was successful before we return data
                if (docResults.StatusCode >= 200 && docResults.StatusCode < 300)
                {
                    //Return the collection of document parameters
                    return docResults.Result;
                }
            }
            catch (SwaggerException swex)
            {
                //Handle any API exceptions.
                if (swex.StatusCode == 404)
                {
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogInformation("AIForgedClient - GetDocumentDataAsync: {time} - API call failed: {swex}", DateTimeOffset.Now, swex);
                }
            }
            //Return null if an exception was raised
            return null;
        }

        /// <summary>
        /// Returns a given document's extraction results as a hierarchical structure.
        /// </summary>
        /// <param name="doc">The document to retrieve the extraction results for.</param>
        /// <returns>Returns a hierarchical collection of DocumentParameterViewModel objects</returns>
        public async Task<ICollection<AIForged.API.DocumentParameterViewModel>> GetDocumentDataHierarchyAsync(DocumentViewModel doc)
        {
            try
            {
                //First we ensure that we are authorized to make an API call.
                bool isAuthorised = await AuthorizeAsync();

                //Stop processing if we are not authorized.
                if (!isAuthorised) return null;

                //Retrieve the latest result documents for given document
                var latestChildDocs = await GetDocumentsAsync(startDate: null,
                    endDate: null,
                    status: null,
                    usage: UsageType.Outbox,
                    masterId: doc.Id,
                    documentId: null);

                //Retrieve the latest result document's extraction results as a hierarchy
                var docResults = await Context.ParametersClient.GetHierarchyAsync(docId: latestChildDocs.FirstOrDefault()?.Id,
                    stpdId: _config.ServiceId,
                    includeverification: true,
                    pageIndex: null);

                //Ensure the API call was successful before we return data
                if (docResults.StatusCode >= 200 && docResults.StatusCode < 300)
                {
                    //Return the collection of document parameters
                    return docResults.Result;
                }
            }
            catch (SwaggerException swex)
            {
                //Handle any API exceptions.
                if (swex.StatusCode == 404)
                {
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogInformation("AIForgedClient - GetDocumentDataHierarchyAsync: {time} - API call failed: {swex}", DateTimeOffset.Now, swex);
                }
            }
            //Return null if an exception was raised
            return null;
        }

        /// <summary>
        /// Update a document's status and add an optional comment
        /// </summary>
        /// <param name="doc">The document to update.</param>
        /// <param name="status">The new document status.</param>
        /// <param name="comment">An optional comment.</param>
        /// <returns></returns>
        public async Task<bool> UpdateDocumentStatusAsync(DocumentViewModel doc, DocumentStatus status, string comment = null)
        {
            try
            {
                //First we ensure that we are authorized to make an API call.
                bool isAuthorised = await AuthorizeAsync();

                //Stop processing if we are not authorized.
                if (!isAuthorised) return false;

                //Update the document object with our new status and optional comment
                doc.Status = status;
                doc.Comment = $"{doc.Comment}\n{comment}";

                //Call the update API and pass in our updated document object
                var updateResult = await Context.DocumentClient.UpdateAsync(doc);

                //Ensure the API call was successful before we return data
                if (updateResult.StatusCode >= 200 && updateResult.StatusCode < 300)
                {
                    //Indicates success
                    return true;
                }
            }
            catch (SwaggerException swex)
            {
                //Handle any API exceptions.
                if (swex.StatusCode == 404)
                {
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogInformation("AIForgedClient - UpdateDocumentStatusAsync: {time} - API call failed: {swex}", DateTimeOffset.Now, swex);
                }
            }
            //Indicates failure
            return false;

        }

        /// <summary>
        /// Recursively search for an return the first result whose parameter definition Id matches the provided Id.
        /// </summary>
        /// <param name="docResults">A collection of DocumentParameterViewModel, preferably a hierarchy.</param>
        /// <param name="paramDefId">The parameter definition Id to match.</param>
        /// <returns></returns>
        public DocumentParameterViewModel GetResultRecursive(ICollection<AIForged.API.DocumentParameterViewModel> docResults, int paramDefId)
        {
            var parameterResult = docResults.FirstOrDefault(p => p.ParamDefId == paramDefId);

            if (parameterResult is not null) return parameterResult;
            foreach (var docResult in docResults)
            {
                parameterResult = GetResultRecursive(docResult.Children, paramDefId);
                if (parameterResult is not null) break;
            }

            return parameterResult;
        }

        /// <summary>
        /// Recursively search for an return the all the results whose parameter definition Id matches the provided Id.
        /// </summary>
        /// <param name="docResults">A collection of DocumentParameterViewModel, preferably a hierarchy.</param>
        /// <param name="paramDefId">The parameter definition Id to match.</param>
        /// <returns></returns>
        public ICollection<DocumentParameterViewModel> GetResultsRecursive(ICollection<AIForged.API.DocumentParameterViewModel> docResults, int paramDefId)
        {
            var results = new List<DocumentParameterViewModel>();
            var parameterResult = docResults.FirstOrDefault(p => p.ParamDefId == paramDefId);

            if (parameterResult is not null)
            {
                results.Add(parameterResult);
            }
            foreach (var docResult in docResults)
            {
                results.AddRange(GetResultsRecursive(docResult.Children, paramDefId));
            }

            return results;
        }
    }
}
