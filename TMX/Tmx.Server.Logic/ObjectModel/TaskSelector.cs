﻿/*
 * Created by SharpDevelop.
 * User: Alexander Petrovskiy
 * Date: 7/22/2014
 * Time: 8:40 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Tmx.Server.Logic.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core;
    using Interfaces;
    using Objects;
    using Tmx.Interfaces.Remoting;

    /// <summary>
    /// Description of TaskSelector.
    /// </summary>
    public class TaskSelector : ITaskSelector
    {
        public virtual List<ITestTask> SelectTasksForClient(Guid clientId, List<ITestTask> tasks)
        {
            var resultTaskScope = new List<ITestTask>();
            
            var client = ClientsCollection.Clients.First(c => c.Id == clientId);
            // TODO: use IDisposable or DI
            // using (var client = ClientsCollection.Clients.First(c => c.Id == clientId)) {
            
            if (null == client) return resultTaskScope;
            
            Trace.TraceInformation("SelectTasksForClient(Guid clientId, List<ITestTask> tasks).1");
            
            // TODO: add IsAdmin and IsInteractive to the checking
            var workflowId = TestRunQueue.TestRuns.First(testRun => testRun.Id == client.TestRunId).WorkflowId;
            
            Trace.TraceInformation("SelectTasksForClient(Guid clientId, List<ITestTask> tasks).2 workflow.Id = {0}", workflowId);
            
            resultTaskScope =
                tasks.Where(task => task.WorkflowId == workflowId)
                .Where(task => // 0 == task.ClientId && 
                                       (Regex.IsMatch((string) (client.CustomString ?? string.Empty), (string) task.Rule) ||
                                        Regex.IsMatch((string) (client.EnvironmentVersion ?? string.Empty), (string) task.Rule) ||
                                        Regex.IsMatch((string) (client.Fqdn ?? string.Empty), (string) task.Rule) ||
                                        Regex.IsMatch((string) (client.Hostname ?? string.Empty), (string) task.Rule) ||
                                        // task.Rule == client.IsAdmin.ToString() ||
                                        // task.Rule == client.IsInteractive.ToString() ||
                                        // Regex.IsMatch(client.OsEdition ?? string.Empty, task.Rule) ||
                                        // Regex.IsMatch(client.OsName ?? string.Empty, task.Rule) ||
                                        Regex.IsMatch((string) (client.OsVersion ?? string.Empty), (string) task.Rule) ||
                                        Regex.IsMatch((string) (client.UserDomainName ?? string.Empty), (string) task.Rule) ||
                                        Regex.IsMatch((string) (client.Username ?? string.Empty), (string) task.Rule))
            ).Select(t => {
                var newTask = t.CloneTaskForNewTestClient();
                newTask.ClientId = clientId;
                return newTask;
            }).ToList();
            
            Trace.TraceInformation("SelectTasksForClient(Guid clientId, List<ITestTask> tasks).4 there are {0} tasks", resultTaskScope.Count);
            
            return resultTaskScope;
        }
        
        public virtual ITestTask GetFirstLegitimateTask(Guid clientId)
        {
            Trace.TraceInformation("GetFirstLegitimateTask(Guid clientId).1");
            
            var taskListForClient = GetOnlyNewTestTasksForClient(clientId);

            var taskArrayForClient = taskListForClient as ITestTask[] ?? taskListForClient.ToArray();
            Trace.TraceInformation("GetFirstLegitimateTask(Guid clientId).2 taskListForClient is null? {0} or empty {1}", null == taskListForClient, !taskArrayForClient.Any());
            
            if (null == taskListForClient || !taskArrayForClient.Any()) return null;
            
            Trace.TraceInformation("GetFirstLegitimateTask(Guid clientId).3");
            
            var taskCandidate = taskArrayForClient.First(task => task.Id == taskArrayForClient.Min(tsk => tsk.Id));
            
            Trace.TraceInformation("GetFirstLegitimateTask(Guid clientId).4 taskCandidate is null? {0}", null == taskCandidate);
            
            return IsItTimeToPublishTask(taskCandidate) ? taskCandidate : null;
        }
        
        public virtual ITestTask GetNextLegitimateTask(Guid clientId, int currentTaskId)
        {
            var taskListForClient = GetOnlyNewTestTasksForClient(clientId);
            var taskArrayForClient = taskListForClient as ITestTask[] ?? taskListForClient.ToArray();
            if (null == taskListForClient || !taskArrayForClient.Any()) return null;
            var tasksToBeNextOne = taskArrayForClient.Where(t => t.Id > currentTaskId);
            var toBeNextOne = tasksToBeNextOne as ITestTask[] ?? tasksToBeNextOne.ToArray();
            return !toBeNextOne.Any() ? null : taskArrayForClient.First(task => task.Id == toBeNextOne.Min(tsk => tsk.Id));
        }
        
        public virtual void CancelFurtherTasksOfTestClient(Guid clientId)
        {
            TaskPool.TasksForClients
                .Where(task => task.ClientId == clientId && !task.IsFinished())
                .ToList()
                .ForEach(task => task.TaskStatus = TestTaskStatuses.Canceled);
        }
        
        public virtual void CancelFurtherTasksOfTestRun(Guid testRunId)
        {
            TaskPool.TasksForClients
                .Where(task => task.TestRunId == testRunId && !task.IsFinished())
                    .ToList()
                    .ForEach(task => task.TaskStatus = TestTaskStatuses.Canceled);
        }
        
        internal virtual IEnumerable<ITestTask> GetOnlyNewTestTasksForClient(Guid clientId)
        {
            Trace.TraceInformation("getOnlyNewTestTasksForClient(Guid clientId).1 client id = {0}", clientId);
            var taskSelection = TaskPool.TasksForClients.Where(task => task.ClientId == clientId && task.IsActive);
            var taskSelectionArray = taskSelection as ITestTask[] ?? taskSelection.ToArray();
            Trace.TraceInformation("getOnlyNewTestTasksForClient(Guid clientId).3 number of tasks for client = {0}", taskSelectionArray.Count());
            Trace.TraceInformation("getOnlyNewTestTasksForClient(Guid clientId).4 number of new tasks for client = {0}", taskSelectionArray.Count(task => task.TaskStatus == TestTaskStatuses.New));
            
            return TaskPool.TasksForClients.Where(task => task.ClientId == clientId && task.IsActive && task.TaskStatus == TestTaskStatuses.New);
        }
        
        internal virtual bool IsItTimeToPublishTask(ITestTask task)
        {
            var numberOfMustDoneBeforeTask = task.AfterTask;
            if (0 == numberOfMustDoneBeforeTask) return true;
            return TaskPool.TasksForClients.Any(t => t.Id == numberOfMustDoneBeforeTask) && !TaskPool.TasksForClients.Any(t => t.Id == numberOfMustDoneBeforeTask && !t.IsFinished());
        }
        
        internal virtual void AddTasksForEveryClient(IEnumerable<ITestTask> activeWorkflowsTasks, Guid testRunId)
        {
            if (0 == ClientsCollection.Clients.Count) return;
            
            foreach (var tasksForClient in from clientId in ClientsCollection.Clients.Where(client => ExtensionMethods.ExtensionMethods.IsInActiveTestRun(client)).Select(client => client.Id)
                                           let workflowsTasks = activeWorkflowsTasks as ITestTask[] ?? activeWorkflowsTasks.ToArray()
                                           // 20150507
                                           // select taskSelector.SelectTasksForClient(clientId, workflowsTasks.ToList()))
                                           select SelectTasksForClient(clientId, workflowsTasks.ToList()))
            {
                tasksForClient.ForEach(task => task.TestRunId = testRunId);
                TaskPool.TasksForClients.AddRange(tasksForClient);
            }
        }
    }
}
