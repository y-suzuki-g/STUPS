﻿/*
 * Created by SharpDevelop.
 * User: Alexander Petrovskiy
 * Date: 10/29/2014
 * Time: 8:27 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Tmx.Client
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using Spring.Rest.Client;
    using Core.Types.Remoting;
    using Interfaces.Remoting;
    using Interfaces.Server;
    
    /// <summary>
    /// Description of TestRunCreator.
    /// </summary>
    public class TestRunCreator
    {
        readonly IRestOperations _restTemplate;
        
        public TestRunCreator(RestRequestCreator requestCreator)
        {
            _restTemplate = requestCreator.GetRestTemplate();
        }
        
        public bool CreateTestRun(string workflowName, TestRunStatuses status, string name)
        {
            Trace.TraceInformation("CreateTestRun(string workflowName, TestRunStatuses status, string name).1");
            
            var testRunCommand = new TestRunCommand {
                TestRunName = name,
                Status = status,
                WorkflowName = workflowName
            };
            
            Trace.TraceInformation("CreateTestRun(string workflowName, TestRunStatuses status, string name).2");
            
            var creatingTestRunResponse = _restTemplate.PostForMessage(UrlList.TestRunsControlPoint_absPath, testRunCommand);
            
            Trace.TraceInformation("CreateTestRun(string workflowName, TestRunStatuses status, string name).3 creatingTestRunResponse is null? {0}", null == creatingTestRunResponse);
            // 20150316
            if (null == creatingTestRunResponse)
                throw  new Exception("Failed to create a test run.");
            
            return HttpStatusCode.Created == creatingTestRunResponse.StatusCode;
        }
    }
}
