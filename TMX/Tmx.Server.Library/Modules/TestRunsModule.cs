﻿/*
 * Created by SharpDevelop.
 * User: Alexander Petrovskiy
 * Date: 10/27/2014
 * Time: 3:24 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Tmx.Server.Library.Modules
{
    using System;
    using System.Dynamic;
    using Core.Types.Remoting;
    using Interfaces.Remoting;
    using Interfaces.Server;
    using Logic.Internal;
    using Logic.ObjectModel;
    using Nancy;
    using Nancy.ModelBinding;
    using Nancy.Responses.Negotiation;

    /// <summary>
    /// Description of TestRunsModule.
    /// </summary>
    public class TestRunsModule : NancyModule
    {
        public TestRunsModule() : base(UrlList.TestRuns_Root)
        {
            Post[UrlList.TestRunsControlPoint_relPath] = _ => CreateNewTestRun(this.Bind<TestRunCommand>());
            Delete[UrlList.TestRuns_One_relPath] = parameters => DeleteTestRun(parameters.id);
            
            // http://blog.nancyfx.org/x-http-method-override-with-nancyfx/
            Put[UrlList.TestRuns_One_Cancel] = parameters => CancelTestRun(parameters.id);
        }
        
        Negotiator CreateNewTestRun(ITestRunCommand testRunCommand)
        {
            var testRunCollectionMethods = ServerObjectFactory.Resolve<TestRunCollectionMethods>();
            testRunCollectionMethods.SetTestRun(testRunCommand, Request.Form);
            var data = testRunCollectionMethods.CreateTestRunExpandoObject();
            return Negotiate.WithStatusCode(HttpStatusCode.OK).WithView(UrlList.ViewTestRuns_TestRunsPageName).WithModel((ExpandoObject)data);
        }
        
        Negotiator DeleteTestRun(Guid testRunId)
        {
            ServerObjectFactory.Resolve<TestRunCollectionMethods>().DeleteTestRun(testRunId);
            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }
        
        Negotiator CancelTestRun(Guid testRunId)
        {
            var testRunCollectionMethods = ServerObjectFactory.Resolve<TestRunCollectionMethods>();
            testRunCollectionMethods.CancelTestRun(testRunId);
            var data = testRunCollectionMethods.CreateTestRunExpandoObject();
            return Negotiate.WithStatusCode(HttpStatusCode.OK).WithView(UrlList.ViewTestRuns_TestRunsPageName).WithModel((ExpandoObject)data);
        }
    }
}
