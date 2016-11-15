﻿using System;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using Dev2.Common;
using Dev2.Common.Common;
using Dev2.Common.ExtMethods;
using Dev2.Common.Interfaces;
using Dev2.Common.Interfaces.Diagnostics.Debug;
using Dev2.Common.Interfaces.Hosting;
using Dev2.Common.Interfaces.Infrastructure;
using Dev2.Communication;
using Dev2.Controller;
using Dev2.Data;
using Dev2.Data.Binary_Objects;
using Dev2.Data.SystemTemplates.Models;
using Dev2.Runtime.ServiceModel.Data;
using Dev2.Data.Util;
using Dev2.Studio.Core;
using Dev2.Studio.Core.Activities.Utils;
using Dev2.Studio.Core.AppResources.Enums;
using Dev2.Studio.Core.AppResources.Repositories;
using Dev2.Studio.Core.Interfaces;
using Dev2.Studio.Core.Models;
using Dev2.Studio.Core.Models.DataList;
using Dev2.Studio.Core.ViewModels;
using Dev2.Studio.ViewModels.DataList;
using Dev2.Threading;
using Dev2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using TechTalk.SpecFlow;
using Unlimited.Applications.BusinessDesignStudio.Activities;
using Warewolf.Studio.ServerProxyLayer;
using Warewolf.Studio.ViewModels;
// ReSharper disable UnusedParameter.Global
// ReSharper disable InconsistentNaming

// ReSharper disable UnusedMember.Global

namespace Dev2.Activities.Specs.TestFramework
{
    internal interface ISpecExternalProcessExecutor : IExternalProcessExecutor
    {
        List<string> WebResult { get; set; }
    }

    internal class SpecExternalProcessExecutor : ISpecExternalProcessExecutor
    {
        #region Implementation of IExternalProcessExecutor

        public SpecExternalProcessExecutor()
        {
            WebResult = new List<string>();
        }

        public void OpenInBrowser(Uri url)
        {

            try
            {
                using (var client = new WebClient())
                {


                    client.Credentials = CredentialCache.DefaultNetworkCredentials;

                    WebResult.Add(client.DownloadString(url));
                }

            }
            catch (Exception e)
            {
                Dev2Logger.Error(e);
            }

        }

        #endregion

        #region Implementation of ISpecExternalProcessExecutor

        public List<string> WebResult { get; set; }

        #endregion
    }

    [Binding]
    public class StudioTestFrameworkSteps
    {
        public StudioTestFrameworkSteps(ScenarioContext scenarioContext)
        {
            if (scenarioContext == null) throw new ArgumentNullException(nameof(scenarioContext));
            MyContext = scenarioContext;
        }

        ScenarioContext MyContext { get; }

        [Given(@"test folder is cleaned")]
        [When(@"test folder is cleaned")]
        public void GivenTestFolderIsCleaned()
        {
            DirectoryHelper.CleanUp(EnvironmentVariables.TestPath);
            var environmentModel = EnvironmentRepository.Instance.Source;
            environmentModel.Connect();
            var commsController = new CommunicationController { ServiceName = "ReloadAllTests" };
            commsController.ExecuteCommand<ExecuteMessage>(environmentModel.Connection, GlobalConstants.ServerWorkspaceID);

            //DirectoryHelper.CleanUp(EnvironmentVariables.TestPath);
            //var environmentModel = EnvironmentRepository.Instance.Source;
            //environmentModel.Connect();
            //((ResourceRepository)environmentModel.ResourceRepository).DeleteAlltests(new List<string>() { "0bdc3207-ff6b-4c01-a5eb-c7060222f75d" });
        }

        [Then(@"test folder is cleaned")]
        public void ThenTestFolderIsCleaned()
        {
            //DirectoryHelper.CleanUp(EnvironmentVariables.TestPath);
            var environmentModel = EnvironmentRepository.Instance.Source;
            environmentModel.Connect();
            ((ResourceRepository)environmentModel.ResourceRepository).DeleteAlltests(new List<string>() { "0bdc3207-ff6b-4c01-a5eb-c7060222f75d" });
        }

        [AfterFeature("@StudioTestFramework")]
        public static void ScenarioCleaning()
        {

            var environmentModel = EnvironmentRepository.Instance.Source;
            environmentModel.Connect();
            ((ResourceRepository)environmentModel.ResourceRepository).DeleteAlltests(new List<string>() { "0bdc3207-ff6b-4c01-a5eb-c7060222f75d" });
        }

        FlowNode CreateFlowNode(Guid id, string displayName)
        {
            return new FlowStep
            {
                Action = new DsfMultiAssignActivity
                {
                    DisplayName = displayName,
                    UniqueID = id.ToString()
                }
            };


        }
        [Given(@"I have ""(.*)"" with inputs as")]
        public void GivenIHaveWithInputsAs(string workflowName, Table inputVariables)
        {
            var environmentModel = EnvironmentRepository.Instance.Source;
            environmentModel.Connect();
            var resourceModel = BuildResourceModel(workflowName, environmentModel);
            MyContext.Add(workflowName + "Resourceid", resourceModel.ID);
            var workflowHelper = new WorkflowHelper();
            var builder = workflowHelper.CreateWorkflow(workflowName);
            if (workflowName.Equals("WorkflowWithTests", StringComparison.InvariantCultureIgnoreCase))
            {
                var flowNode = CreateFlowNode(resourceModel.ID, workflowName);
                ((Flowchart)builder.Implementation).StartNode = flowNode;
            }

            resourceModel.WorkflowXaml = workflowHelper.GetXamlDefinition(builder);

            var datalistViewModel = new DataListViewModel();
            datalistViewModel.InitializeDataListViewModel(resourceModel);
            foreach (var variablesRow in inputVariables.Rows)
            {
                AddVariables(variablesRow["Input Var Name"], datalistViewModel, enDev2ColumnArgumentDirection.Input);
            }
            datalistViewModel.WriteToResourceModel();
            MyContext.Add(workflowName, resourceModel);
            MyContext.Add($"{workflowName}dataListViewModel", datalistViewModel);
            if (!MyContext.ContainsKey("popupController"))
            {
                var popupController = new Mock<Common.Interfaces.Studio.Controller.IPopupController>();
                popupController.Setup(controller => controller.ShowDeleteConfirmation(It.IsAny<string>())).Returns(MessageBoxResult.Yes);
                popupController.Setup(controller => controller.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.OK, MessageBoxImage.Error, null, false, true, false, false)).Verifiable();
                popupController.Setup(controller => controller.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.OK, MessageBoxImage.Information, null, false, true, false, false)).Verifiable();
                CustomContainer.Register(popupController.Object);
                MyContext["popupController"] = popupController;
            }
            var shellViewModel = new Mock<IShellViewModel>();
            shellViewModel.Setup(model => model.CloseResourceTestView(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()));
            CustomContainer.Register(shellViewModel.Object);
            MyContext["shellViewModel"] = shellViewModel;
        }

        private static ResourceModel BuildResourceModel(string workflowName, IEnvironmentModel environmentModel)
        {
            var newGuid = Guid.NewGuid();
            var resourceModel = new ResourceModel(environmentModel)
            {
                ResourceName = workflowName,
                DisplayName = workflowName,
                DataList = "",
                ID = newGuid,
                Category = workflowName
            };
            return resourceModel;

        }

        readonly object _syncRoot = new object();
        const string Json = "{\"$type\":\"Dev2.Data.ServiceTestModelTO,Dev2.Data\",\"OldTestName\":null,\"TestName\":\"Test 1\",\"UserName\":null,\"Password\":null,\"LastRunDate\":\"0001-01-01T00:00:00\",\"Inputs\":null,\"Outputs\":null,\"NoErrorExpected\":false,\"ErrorExpected\":false,\"TestPassed\":false,\"TestFailing\":false,\"TestInvalid\":false,\"TestPending\":false,\"Enabled\":true,\"IsDirty\":false,\"AuthenticationType\":0,\"ResourceId\":\"00000000-0000-0000-0000-000000000000\"}";

        [Given(@"I have a resouce ""(.*)""")]
        public void GivenIHaveAResouce(string resourceName)
        {
            _resourceForTests = resourceName;
            var resourceId = Guid.NewGuid();
            // ReSharper disable once UnusedVariable
            var environmentModel = EnvironmentRepository.Instance.Source;
            var resourceModel = new ResourceModel(environmentModel)
            {
                ResourceName = resourceName,
                DisplayName = resourceName,
                DataList = "",
                ID = resourceId,
                Category = resourceName
            };
            var workflowHelper = new WorkflowHelper();
            var builder = workflowHelper.CreateWorkflow(resourceName);
            resourceModel.WorkflowXaml = workflowHelper.GetXamlDefinition(builder);

            environmentModel.ResourceRepository.SaveToServer(resourceModel);

            ScenarioContext.Current.Add(resourceName + "id", resourceId);
        }
        string _resourceForTests = "";
        [Given(@"I add ""(.*)"" as tests")]
        public void GivenIAddAsTests(string p0)
        {

            var environmentModel = EnvironmentRepository.Instance.Source;
            var serviceTestModelTos = new List<IServiceTestModelTO>();
            environmentModel.ResourceRepository.ForceLoad();
            var savedSource = environmentModel.ResourceRepository.All().First(model => model.ResourceName.Equals(_resourceForTests, StringComparison.InvariantCultureIgnoreCase));
            MyContext["PluginSource" + "id"] = savedSource.ID;

            var resourceID = MyContext.Get<Guid>("PluginSourceid");
            lock (_syncRoot)
            {
                var testNamesNames = p0.Split(',');
                foreach (var resourceName in testNamesNames)
                {
                    Dev2JsonSerializer serializer = new Dev2JsonSerializer();
                    var serviceTestModelTO = serializer.Deserialize<ServiceTestModelTO>(Json);
                    serviceTestModelTO.TestName = resourceName;
                    serviceTestModelTO.ResourceId = resourceID;
                    serviceTestModelTO.Inputs = new List<IServiceTestInput>();
                    serviceTestModelTO.Outputs = new List<IServiceTestOutput>();
                    serviceTestModelTO.AuthenticationType = AuthenticationType.Windows;

                    serviceTestModelTos.Add(serviceTestModelTO);
                }
            }
            // ReSharper disable once UnusedVariable
            var resourceModel = new ResourceModel(environmentModel)
            {
                ID = savedSource.ID,
                Category = "",
                ResourceName = _resourceForTests
            };
            environmentModel.ResourceRepository.SaveTests(resourceModel, serviceTestModelTos);
        }
        const string ResourceCat = "ResourceCat\\";
        [Then(@"""(.*)"" has (.*) tests")]
        public void ThenHasTests(string resourceName, int numberOdTests)
        {
            var environmentModel = EnvironmentRepository.Instance.Source;
            var resourceID = MyContext.Get<Guid>(resourceName + "id");
            var serviceTestModelTos = environmentModel.ResourceRepository.LoadResourceTests(resourceID);
            Assert.AreEqual(numberOdTests, serviceTestModelTos.Count);
        }

        [When(@"I delete resource ""(.*)""")]
        public void WhenIDeleteResource(string resourceName)
        {
            var environmentModel = EnvironmentRepository.Instance.Source;
            MyContext.Get<Guid>(resourceName + "id");
            // ReSharper disable once UnusedVariable
            var savedSource = environmentModel.ResourceRepository.All().First(model => model.ResourceName.Equals(_resourceForTests, StringComparison.InvariantCultureIgnoreCase));
            environmentModel.ResourceRepository.DeleteResource(savedSource);

        }

        private static void AddVariables(string variableName, DataListViewModel datalistViewModel, enDev2ColumnArgumentDirection ioDirection)
        {

            if (DataListUtil.IsValueScalar(variableName))
            {
                var scalarName = DataListUtil.RemoveLanguageBrackets(variableName);
                var scalarItemModel = new ScalarItemModel(scalarName, ioDirection);
                if (!scalarItemModel.HasError)
                {
                    datalistViewModel.ScalarCollection.Add(scalarItemModel);
                }
            }
            if (DataListUtil.IsValueRecordsetWithFields(variableName))
            {
                var rsName = DataListUtil.ExtractRecordsetNameFromValue(variableName);
                var fieldName = DataListUtil.ExtractFieldNameOnlyFromValue(variableName);
                var rs = datalistViewModel.RecsetCollection.FirstOrDefault(model => model.Name == rsName);
                if (rs == null)
                {
                    var recordSetItemModel = new RecordSetItemModel(rsName);
                    datalistViewModel.RecsetCollection.Add(recordSetItemModel);
                    recordSetItemModel.Children.Add(new RecordSetFieldItemModel(fieldName,
                        recordSetItemModel, ioDirection));
                }
                else
                {
                    var recordSetFieldItemModel = rs.Children.FirstOrDefault(model => model.Name == fieldName);
                    if (recordSetFieldItemModel == null)
                    {
                        rs.Children.Add(new RecordSetFieldItemModel(fieldName, rs, ioDirection));
                    }
                }
            }
        }
        [Given(@"""(.*)"" Tests as")]
        public void GivenTestsAs(string workFlowName, Table table)
        {
            var resourceIdKey = workFlowName + "Resourceid";
            var resourceID = MyContext.Get<Guid>(resourceIdKey);
            var environmentModel = EnvironmentRepository.Instance.Source;
            List<IServiceTestModelTO> serviceTestModelTos = new List<IServiceTestModelTO>();
            foreach (var tableRow in table.Rows)
            {

                Dev2JsonSerializer serializer = new Dev2JsonSerializer();
                var serviceTestModelTO = serializer.Deserialize<ServiceTestModelTO>(Json);
                serviceTestModelTO.TestName = tableRow["TestName"];
                serviceTestModelTO.ResourceId = resourceID;
                serviceTestModelTO.TestFailing = bool.Parse(tableRow["TestFailing"]);
                serviceTestModelTO.TestPending = bool.Parse(tableRow["TestPending"]);
                serviceTestModelTO.TestInvalid = bool.Parse(tableRow["TestInvalid"]);
                serviceTestModelTO.TestPassed = bool.Parse(tableRow["TestPassed"]);
                serviceTestModelTO.Inputs = new List<IServiceTestInput>();
                serviceTestModelTO.Outputs = new List<IServiceTestOutput>();
                serviceTestModelTO.AuthenticationType = AuthenticationType.Windows;

                serviceTestModelTos.Add(serviceTestModelTO);

            }
            // ReSharper disable once UnusedVariable
            var resourceModel = new ResourceModel(environmentModel)
            {
                ID = resourceID,
                Category = "",
                ResourceName = _resourceForTests
            };
            environmentModel.ResourceRepository.SaveTests(resourceModel, serviceTestModelTos);
        }

        [Then(@"""(.*)"" is passing")]
        public void ThenIsPassing(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => model.TestName.Equals(testName, StringComparison.InvariantCultureIgnoreCase));


            Assert.IsTrue(serviceTestModel.TestPassed);
        }

        [Then(@"""(.*)"" is failing")]
        public void ThenIsFailing(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => model.TestName.Equals(testName, StringComparison.InvariantCultureIgnoreCase));
            Assert.IsTrue(serviceTestModel.TestFailing);
        }

        [Then(@"""(.*)"" is invalid")]
        public void ThenIsInvalid(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => model.TestName.Equals(testName, StringComparison.InvariantCultureIgnoreCase));
            Assert.IsTrue(serviceTestModel.TestInvalid);
        }

        [Given(@"there are no tests")]
        [When(@"there are no tests")]
        [Then(@"there are no tests")]
        public void GivenThereAreNoTests()
        {
            var serviceTest = GetTestForCurrentTestFramework();
            Assert.AreEqual(0, serviceTest.Count());
        }

        [Then(@"""(.*)"" is pending")]
        public void ThenIsPending(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => model.TestName.Equals(testName, StringComparison.InvariantCultureIgnoreCase));
            Assert.IsTrue(serviceTestModel.TestPending);
        }
        [Then(@"debug window is visible")]
        public void ThenDebugWindowIsVisible()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var count = serviceTest.SelectedServiceTest.DebugForTest.Any(state => state.DisplayName == "Hello World");
            //var count = serviceTest.SelectedServiceTest.DebugForTest.All(state => state.DisplayName == "WorkflowWithTests");
            Assert.IsTrue(count);
        }



        [Given(@"""(.*)"" has outputs as")]
        public void GivenHasOutputsAs(string workflowName, Table outputVariables)
        {
            ResourceModel resourceModel;
            if (MyContext.TryGetValue(workflowName, out resourceModel))
            {
                DataListViewModel dataListViewModel;
                if (MyContext.TryGetValue($"{workflowName}dataListViewModel", out dataListViewModel))
                {
                    foreach (var variablesRow in outputVariables.Rows)
                    {
                        AddVariables(variablesRow["Ouput Var Name"], dataListViewModel, enDev2ColumnArgumentDirection.Output);
                    }
                    dataListViewModel.WriteToResourceModel();
                    var environmentModel = EnvironmentRepository.Instance.Source;
                    environmentModel.ResourceRepository.SaveToServer(resourceModel);
                }
                else
                {
                    Assert.Fail("No Datalist found");
                }
            }
            else
            {
                Assert.Fail($"Resource Model for {workflowName} not found");
            }
        }



        [Given(@"the test builder is open with ""(.*)""")]
        [When(@"the test builder is open with ""(.*)""")]
        [Then(@"the test builder is open with ""(.*)""")]
        public void GivenTheTestBuilderIsOpenWith(string workflowName)
        {
            ResourceModel resourceModel;
            if (MyContext.TryGetValue(workflowName, out resourceModel))
            {
                var vm = new ServiceTestViewModel(resourceModel, new SynchronousAsyncWorker(), new Mock<IEventAggregator>().Object, new SpecExternalProcessExecutor(), new Mock<IWorkflowDesignerViewModel>().Object);
                Assert.IsNotNull(vm);
                Assert.IsNotNull(vm.ResourceModel);
                MyContext.Add("testFramework", vm);
                return;
            }
            var resourceId = ConfigurationManager.AppSettings[workflowName].ToGuid();
            var loadContextualResourceModel = EnvironmentRepository.Instance.Source.ResourceRepository.LoadContextualResourceModel(resourceId);
            var testFramework = new ServiceTestViewModel(loadContextualResourceModel, new SynchronousAsyncWorker(), new Mock<IEventAggregator>().Object, new SpecExternalProcessExecutor(), new Mock<IWorkflowDesignerViewModel>().Object);
            Assert.IsNotNull(testFramework);
            Assert.IsNotNull(testFramework.ResourceModel);
            MyContext.Add("testFramework", testFramework);


        }
        
        [Given(@"I update inputs as")]
        [When(@"I update inputs as")]
        [Then(@"I update inputs as")]
        public void WhenIUpdatedTheInputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var inputs = serviceTest.SelectedServiceTest.Inputs;
            foreach (var tableRow in table.Rows)
            {
                var valueToSet = tableRow["Value"];
                if (!string.IsNullOrEmpty(valueToSet))
                {
                    var varName = tableRow["Variable Name"];
                    var foundInput = inputs.FirstOrDefault(input => input.Variable == varName);
                    if (foundInput != null)
                    {
                        foundInput.Value = valueToSet;
                    }
                }
            }
        }


        [Then(@"I update outputs as")]
        public void ThenIUpdateOutputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var outputs = serviceTest.SelectedServiceTest.Outputs;
            foreach (var tableRow in table.Rows)
            {
                var valueToSet = tableRow["Value"];
                if (!string.IsNullOrEmpty(valueToSet))
                {
                    var varName = tableRow["Variable Name"];
                    var foundInput = outputs.FirstOrDefault(output => output.Variable == varName);
                    if (foundInput != null)
                    {
                        foundInput.Value = valueToSet;
                    }
                }
            }
        }
        [Given(@"Tab Header is ""(.*)""")]
        [When(@"Tab Header is ""(.*)""")]
        [Then(@"Tab Header is ""(.*)""")]
        public void GivenTabHeaderIs(string expectedTabHeader)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(expectedTabHeader, serviceTest.DisplayName);
        }

        [When(@"I run the test")]
        public void WhenIRunTheTest()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunSelectedTestCommand.Execute(null);
        }

        [When(@"I run selected test in Web")]
        public void WhenIRunSelectedTestInWeb()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunSelectedTestInBrowserCommand.Execute(null);
        }

        [Then(@"all tests pass")]
        public void ThenAllTestsPass()
        {
            var testModels = GetTestForCurrentTestFramework();
            var allPassed = testModels.All(model => model.TestPassed);
            Assert.IsTrue(allPassed);
        }


        [Then(@"I run all tests")]
        public void ThenIRunAllTests()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunAllTestsCommand.Execute(null);
        }



        [When(@"I run all tests in Web")]
        public void WhenIRunAllTestsInWeb()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunAllTestsInBrowserCommand.Execute(null);
        }


        [Then(@"test result is Passed")]
        public void ThenTestResultIsPassed()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            Assert.IsNotNull(test);
            Assert.IsTrue(test.TestPassed);
            Assert.IsFalse(test.TestFailing);
        }

        [Then(@"test result is invalid")]
        public void ThenTestResultIsInvalid()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            Assert.IsNotNull(test);
            Assert.IsTrue(test.TestInvalid);
        }


        [Then(@"test result is Failed")]
        public void ThenTestResultIsFailed()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            Assert.IsNotNull(test);
            Assert.IsFalse(test.TestPassed);
            Assert.IsTrue(test.TestFailing);
        }

        [When(@"I remove input ""(.*)"" from workflow ""(.*)""")]
        public void WhenIRemoveInputFromWorkflow(string input, string workflow)
        {
            var resourceIdKey = workflow + "Resourceid";
            var environmentModel = EnvironmentRepository.Instance.Source;
            var resourceId = MyContext.Get<Guid>(resourceIdKey);
            var resourceToChange = environmentModel.ResourceRepository.FindResourcesByID(environmentModel, new[] { resourceId.ToString() }, ResourceType.WorkflowService).Single();
            var newDatalist = resourceToChange.DataList.Replace(input, input + "Newname");
            resourceToChange.DataList = newDatalist;
            _resourceModelWithDifInputs = resourceToChange;

        }
        IResourceModel _resourceModelWithDifInputs;

        [When(@"I save Workflow\t""(.*)""")]
        public void WhenISaveWorkflow(string workflow)
        {
            var environmentModel = EnvironmentRepository.Instance.Source;
            environmentModel.ResourceRepository.SaveToServer(_resourceModelWithDifInputs);
        }

        [When(@"all test are invalid")]
        public void WhenAllTestAreInvalid()
        {
            var serviceTestModels = GetTestForCurrentTestFramework();
            var allInvalid = serviceTestModels.All(model => model.TestInvalid);
            Assert.IsTrue(allInvalid);
        }

        [Then(@"the tab is closed")]
        public void ThenTheTabIsClosed()
        {
            Mock<IShellViewModel> shellViewModel = MyContext.Get<Mock<IShellViewModel>>("shellViewModel");
            shellViewModel.Verify(vm => vm.CloseResourceTestView(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()));
        }

        [Then(@"The ""(.*)"" popup is shown I click Ok")]
        public void ThenThePopupIsShownIClickOk(string popupViewName)
        {
            Mock<Common.Interfaces.Studio.Controller.IPopupController> popupController = MyContext.Get<Mock<Common.Interfaces.Studio.Controller.IPopupController>>("popupController");

            switch (popupViewName)
            {
                case "Delete Confirmation":
                    popupController.Verify(controller => controller.ShowDeleteConfirmation(It.IsAny<string>()));
                    break;
                case "Workflow Deleted":
                    popupController.Verify(controller => controller.Show(Warewolf.Studio.Resources.Languages.Core.ServiceTestResourceDeletedMessage, Warewolf.Studio.Resources.Languages.Core.ServiceTestResourceDeletedHeader, It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()));
                    break;
                case "Workflow changed":
                    popupController.Verify(controller => controller.Show(Warewolf.Studio.Resources.Languages.Core.ServiceTestResourceCategoryChangedMessage, Warewolf.Studio.Resources.Languages.Core.ServiceTestResourceCategoryChangedHeader, It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()));
                    break;
            }
        }

        [Given(@"I click New Test")]
        [When(@"I click New Test")]
        [Then(@"I click New Test")]
        public void WhenIClickNewTest()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.CreateTestCommand.Execute(null);

        }


        [Then(@"a new test is added")]
        public void ThenANewTestIsAdded()
        {
            var currentTests = GetTestForCurrentTestFramework();
            Assert.AreNotEqual(0, currentTests.Count());
        }

        [Then(@"Test Status is ""(.*)""")]
        public void ThenTestStatusIs(string expectedStatus)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();

            switch (expectedStatus)
            {
                case "TestPending":
                    Assert.IsTrue(serviceTest.Tests[0].TestPending);
                    break;
                case "TestPassed":
                    Assert.IsTrue(serviceTest.Tests[0].TestPassed);
                    break;
                case "TestFailing":
                    Assert.IsTrue(serviceTest.Tests[0].TestFailing);
                    break;
                case "TestInvalid":
                    Assert.IsTrue(serviceTest.Tests[0].TestInvalid);
                    break;
                default:
                    Assert.IsTrue(serviceTest.Tests[0].TestPending);
                    break;
            }
        }

        [Then(@"""(.*)"" test is visible")]
        public void ThenTestIsVisible(string testCommand)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();

            switch (testCommand)
            {
                case "Stop":
                    Assert.IsTrue(serviceTest.StopTestCommand.CanExecute(null));
                    break;
                case "Run":
                    Assert.IsTrue(serviceTest.RunSelectedTestCommand.CanExecute(null));
                    break;
                case "Save":
                    Assert.IsTrue(serviceTest.CanSave);
                    break;
                default:
                    Assert.Fail("Incorrect Command Option!");
                    break;
            }
        }

        [Then(@"there are (.*) tests")]
        public void ThenThereAreTests(int testCount)
        {
            var currentTests = GetTestForCurrentTestFramework();
            Assert.AreEqual(testCount, currentTests.Count());
        }

        [Then(@"test name starts with ""(.*)""")]
        public void ThenTestNameStartsWith(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(testName, serviceTest.SelectedServiceTest.TestName);
        }

        [Then(@"""(.*)"" is selected")]
        public void ThenIsSelected(string testName)
        {
            var serviceTest = GetTestFrameworkFromContext();
            if (testName == "Dummy Test")
            {
                Assert.IsNull(serviceTest.SelectedServiceTest);
            }
            else
            {
                Assert.AreEqual(testName, serviceTest.SelectedServiceTest.TestName);
            }
        }


        [Then(@"username is blank")]
        public void ThenUsernameIsBlank()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(null, serviceTest.SelectedServiceTest.UserName);
        }

        [Then(@"password is blank")]
        public void ThenPasswordIsBlank()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(null, serviceTest.SelectedServiceTest.Password);
        }

        [Then(@"inputs are")]
        public void ThenInputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var inputs = serviceTest.SelectedServiceTest.Inputs;
            Assert.AreNotEqual(0, inputs.Count);
            var i = 0;
            foreach (var tableRow in table.Rows)
            {
                Assert.AreEqual(tableRow["Variable Name"], inputs[i].Variable);
                var expected = tableRow["Value"];
                Assert.AreEqual(expected, inputs[i].Value);
                i++;
            }

        }


        [Then(@"outputs as")]
        public void ThenOutputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var outputs = serviceTest.SelectedServiceTest.Outputs;
            Assert.AreNotEqual(0, outputs.Count);
            var i = 0;
            foreach (var tableRow in table.Rows)
            {
                Assert.AreEqual(tableRow["Variable Name"], outputs[i].Variable);
                var expected = tableRow["Value"];
                Assert.AreEqual(expected, outputs[i].Value);
                i++;
            }
        }



        [Then(@"The WebResponse as")]
        public void ThenTheWebResponseAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var fieldInfo = typeof(ServiceTestViewModel).GetField("_processExecutor", BindingFlags.NonPublic | BindingFlags.Instance);
            var specExternalProcessExecutor = fieldInfo?.GetValue(serviceTest) as ISpecExternalProcessExecutor;
            if (specExternalProcessExecutor != null)
            {
                var webResult = specExternalProcessExecutor.WebResult;
                foreach (var result in webResult)
                {
                    var cleanresult = result.Replace("[", "").Replace("]", "");
                    var jObject = JObject.Parse(cleanresult);

                    foreach (var tableRow in table.Rows)
                    {
                        foreach (var resultPairs in jObject)
                        {
                            // Assert.AreEqual(tableRow["Test Name"], resultPairs);
                            if (resultPairs.Key == "Test Name")
                            {
                                Assert.AreEqual(tableRow["Test Name"], resultPairs.Value, "value message dont match");
                            }
                            if (resultPairs.Key == "Result")
                            {
                                Assert.AreEqual(tableRow["Result"], resultPairs.Value, "Result message dont match");
                            }

                            if (resultPairs.Key == "Message")
                            {
                                Assert.AreEqual(tableRow["Message"], resultPairs.Value.ToString().Replace("\n", "").Replace("\r", "").Replace(Environment.NewLine, ""), "error message dont match");
                            }
                        }
                    }
                }
            }
        }



        [When(@"""(.*)"" is deleted")]
        public void WhenIsDeleted(string workflowName)
        {
            ResourceModel resourceModel;
            if (MyContext.TryGetValue(workflowName, out resourceModel))
            {
                var env = EnvironmentRepository.Instance.Source;
                env.ResourceRepository.DeleteResource(resourceModel);
            }
        }

        [When(@"""(.*)"" is moved")]
        public void WhenIsMoved(string workflowName)
        {
            ResourceModel resourceModel;
            if (MyContext.TryGetValue(workflowName, out resourceModel))
            {
                var env = EnvironmentRepository.Instance.Source;
                resourceModel.Category = "bob\\" + workflowName;
                env.ResourceRepository.SaveToServer(resourceModel);
            }
        }

        [Given(@"save is enabled")]
        [Then(@"save is disabled")]
        public void ThenSaveIsDisabled()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsFalse(serviceTest.CanSave);
        }
        [Then(@"save is enabled")]
        [When(@"save is enabled")]
        public void ThenSaveIsEnabled()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsTrue(serviceTest.CanSave);
        }

        [Then(@"test status is pending")]
        public void ThenTestStatusIsPending()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsTrue(serviceTest.SelectedServiceTest.TestPending);

        }

        [Then(@"test is enabled")]
        public void ThenTestIsEnabled()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsTrue(serviceTest.SelectedServiceTest.Enabled);
        }

        [Given(@"I save")]
        [When(@"I save")]
        [Then(@"I save")]
        public void WhenISave()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.Save();
        }

        [Then(@"I close the test builder")]
        public void ThenICloseTheTestBuilder()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest?.Dispose();
            ScenarioContext.Current.Remove("testFramework");
        }


        [Then(@"Inputs are empty")]
        public void ThenInputsAreEmpty()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var hasNoInputs = serviceTest.SelectedServiceTest.Inputs == null;
            Assert.IsTrue(hasNoInputs);
        }

        [Then(@"Outputs are empty")]
        public void ThenOutputsAreEmpty()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var hasNoOutputs = serviceTest.SelectedServiceTest.Outputs == null;
            Assert.IsTrue(hasNoOutputs);
        }

        [Then(@"No Error selected")]
        public void ThenNoErrorSelected()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var noErrorExpected = serviceTest.SelectedServiceTest.NoErrorExpected;
            Assert.IsTrue(noErrorExpected);
        }

        [When(@"I change the test name to ""(.*)""")]
        public void WhenIChangeTheTestNameTo(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.SelectedServiceTest.TestName = testName;
        }

        [Then(@"I set ErrorExpected to ""(.*)""")]
        public void ThenISetErrorExpectedTo(string value)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            if (value == "true")
                serviceTest.SelectedServiceTest.ErrorExpected = true;
        }

        [Then(@"change ErrorContainsText to ""(.*)""")]
        public void ThenChangeErrorContainsTextTo(string errorContainsText)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.SelectedServiceTest.ErrorContainsText = errorContainsText;
        }

        [Then(@"test URL is ""(.*)""")]
        public void ThenTestURLIs(string testUrl)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.SelectedServiceTest.RunSelectedTestUrl = testUrl;
        }

        [Then(@"Test name is ""(.*)""")]
        public void ThenTestNameIs(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(testName, serviceTest.SelectedServiceTest.TestName);
        }

        [Then(@"Name for display is ""(.*)"" and test is edited")]
        public void ThenNameForDisplayIsAndTestIsEdited(string nameForDisplay)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsTrue(serviceTest.SelectedServiceTest.IsDirty);
            Assert.AreEqual(nameForDisplay, serviceTest.SelectedServiceTest.NameForDisplay);
        }

        [Then(@"Name for display is ""(.*)"" and test is not edited")]
        public void ThenNameForDisplayIs(string nameForDisplay)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsFalse(serviceTest.SelectedServiceTest.IsDirty);
            Assert.AreEqual(nameForDisplay, serviceTest.SelectedServiceTest.NameForDisplay);
        }

        [When(@"I ""(.*)"" the selected test")]
        public void WhenITheSelectedTest(string status)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.SelectedServiceTest.Enabled = status == "Enable";
        }

        [Then(@"DeleteCommand is ""(.*)""")]
        public void ThenDeleteCommandIs(string status)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            if (status == "Active")
            {
                Assert.IsTrue(serviceTest.DeleteTestCommand.CanExecute(null));
            }
            else
            {
                Assert.IsFalse(serviceTest.DeleteTestCommand.CanExecute(null));
            }
        }

        [Given(@"I select ""(.*)""")]
        [When(@"I select ""(.*)""")]
        [Then(@"I select ""(.*)""")]
        public void GivenISelect(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => string.Equals(model.TestName, testName, StringComparison.InvariantCultureIgnoreCase));
            serviceTest.SelectedServiceTest = serviceTestModel;
        }

        [Given(@"I set Test Values as")]
        [When(@"I set Test Values as")]
        [Then(@"I set Test Values as")]
        public void GivenISetTestValuesAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            foreach (var tableRow in table.Rows)
            {
                var testName = tableRow["TestName"];
                var authenticationType = tableRow["AuthenticationType"];
                AuthenticationType authent;
                Enum.TryParse(authenticationType, true, out authent);
                var error = tableRow["Error"];
                serviceTest.SelectedServiceTest.TestName = testName;
                serviceTest.SelectedServiceTest.ErrorExpected = bool.Parse(error);
                serviceTest.SelectedServiceTest.AuthenticationType = authent;

            }
        }

        [Then(@"Test Status saved is ""(.*)""")]
        public void ThenTestStatusSavedIs(string testStatus)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();

            switch (testStatus)
            {
                case "TestPending":
                    serviceTest.SelectedServiceTest.TestPending = true;
                    break;
                case "TestInvalid":
                    serviceTest.SelectedServiceTest.TestInvalid = true;
                    break;
                case "TestFailing":
                    serviceTest.SelectedServiceTest.TestFailing = true;
                    break;
                case "TestPassed":
                    serviceTest.SelectedServiceTest.TestPassed = true;
                    break;
                default:
                    serviceTest.SelectedServiceTest.TestPending = true;
                    break;
            }
        }

        [Then(@"NoErrorExpected is ""(.*)""")]
        public void ThenNoErrorExpectedIs(string error)
        {
            var hasError = bool.Parse(error);
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(hasError, serviceTest.SelectedServiceTest.NoErrorExpected);

        }

        [Then(@"Authentication is Public")]
        public void ThenAuthenticationIsPublic()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.AreEqual(AuthenticationType.Public, serviceTest.SelectedServiceTest.AuthenticationType);
        }

        [When(@"I disable ""(.*)""")]
        public void WhenIDisable(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => string.Equals(model.TestName, testName, StringComparison.InvariantCultureIgnoreCase));
            serviceTestModel.Enabled = false;
        }

        [When(@"I enable ""(.*)""")]
        public void WhenIEnable(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => string.Equals(model.TestName, testName, StringComparison.InvariantCultureIgnoreCase));
            serviceTestModel.Enabled = true;
        }


        [Then(@"Delete is disabled for ""(.*)""")]
        public void ThenDeleteIsDisabledFor(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => string.Equals(model.TestName, testName, StringComparison.InvariantCultureIgnoreCase));
            var canDelete = serviceTest.DeleteTestCommand.CanExecute(serviceTestModel);
            Assert.IsFalse(canDelete);
        }

        [Then(@"Delete is enabled for ""(.*)""")]
        public void ThenDeleteIsEnabledFor(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => string.Equals(model.TestName, testName, StringComparison.InvariantCultureIgnoreCase));
            var canDelete = serviceTest.DeleteTestCommand.CanExecute(serviceTestModel);
            Assert.IsTrue(canDelete);
        }


        [Given(@"I set inputs as")]
        [When(@"I set inputs as")]
        [Then(@"I set inputs as")]
        public void GivenISetInputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();

            foreach (var tableRow in table.Rows)
            {
                var vname = tableRow["Variable Name"];
                var value = tableRow["Value"];
                serviceTest.SelectedServiceTest.Inputs.Add
                    (
                            new ServiceTestInput(vname, value)
                    );
            }
        }

        [Given(@"I set outputs as")]
        [When(@"I set outputs as")]
        [Then(@"I set outputs as")]
        public void GivenISetOutputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();

            foreach (var tableRow in table.Rows)
            {
                var vname = tableRow["Variable Name"];
                var value = tableRow["Value"];
                var from = tableRow["From"];
                var to = tableRow["To"];
                serviceTest.SelectedServiceTest.Outputs.Add
                    (
                       new ServiceTestOutput(vname, value, from, to)
                    );
            }
        }

        [Then(@"Delete is enabled")]
        public void ThenDeleteIsEnabled()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var canDelete = serviceTest.DeleteTestCommand.CanExecute(null);
            Assert.IsTrue(canDelete);
        }

        [Then(@"Run is enabled")]
        public void ThenRunIsEnabled()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var canDelete = serviceTest.RunSelectedTestCommand.CanExecute(null);
            Assert.IsTrue(canDelete);
        }

        [When(@"I delete ""(.*)""")]
        public void WhenIDelete(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => string.Equals(model.TestName, testName, StringComparison.InvariantCultureIgnoreCase));
            serviceTest.DeleteTestCommand.Execute(serviceTestModel);
        }

        [When(@"I delete selected Test")]
        public void WhenIDeleteSelectedTest()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.DeleteTestCommand.Execute(null);

        }

        [Then(@"The Confirmation popup is shown")]
        public void ThenTheConfirmationPopupIsShown()
        {
            var mock = MyContext["popupController"] as Mock<Common.Interfaces.Studio.Controller.IPopupController>;
            // ReSharper disable once PossibleNullReferenceException
            mock.VerifyAll();
        }

        [Then(@"The Pending Changes Confirmation popup is shown I click Ok")]
        public void ThenThePendingChangesConfirmationPopupIsShownIClickOk()
        {
            var mock = (Mock<Common.Interfaces.Studio.Controller.IPopupController>)MyContext["popupController"];
            mock.Verify(controller => controller.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.OK, MessageBoxImage.Information, null, false, true, false, false));
        }

        [Then(@"Error is ""(.*)""")]
        public void ThenErrorIs(string hasError)
        {
            var error = bool.Parse(hasError);
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var errorExpected = serviceTest.SelectedServiceTest.ErrorExpected;
            Assert.AreEqual(error, errorExpected);
        }



        [When(@"test is disabled")]
        public void WhenTestIsDisabled()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var enabled = serviceTest.SelectedServiceTest.Enabled;
            Assert.IsFalse(enabled);
        }

        [When(@"I click ""(.*)""")]
        public void WhenIClick(string testName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var serviceTestModel = serviceTest.Tests.Single(model => model.TestName == testName);
            serviceTest.SelectedServiceTest = serviceTestModel;
        }


        [Then(@"Duplicate Test is visible")]
        public void ThenDuplicateTestIsVisible()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var canExecute = serviceTest.DuplicateTestCommand.CanExecute(null);
            Assert.IsTrue(canExecute);
        }

        [When(@"I run selected test")]
        public void WhenIRunSelectedTest()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunSelectedTestCommand.Execute(null);
        }

        [When(@"I run selected test in browser")]
        public void WhenIRunSelectedTestBrowser()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunSelectedTestInBrowserCommand.Execute(null);
        }

        [When(@"selected test is empty")]
        public void WhenSelectedTestIsEmpty()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsNull(serviceTest.SelectedServiceTest);
        }

        [When(@"I run all tests")]
        public void WhenIRunAllTests()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunAllTestsCommand.Execute(null);
        }

        [When(@"I run all tests in browser")]
        public void WhenIRunAllTestsInBrowser()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.RunAllTestsInBrowserCommand.Execute(null);
        }

        [When(@"I click delete test")]
        public void WhenIClickDeleteTest()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.DeleteTestCommand.Execute(null);
        }

        [When(@"I click duplicate")]
        public void WhenIClickDuplicate()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            serviceTest.DuplicateTestCommand.Execute(null);
        }

        [Then(@"the duplicated tests is ""(.*)""")]
        public void ThenTheDuplicatedTestsIs(string dupTestName)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            Assert.IsTrue(serviceTest.SelectedServiceTest.TestName == dupTestName);
            var count = serviceTest.Tests.Count(model => dupTestName.Contains(model.TestName));
            Assert.AreEqual(2, count);
        }
        [Then(@"Duplicate Test in not Visible")]
        public void ThenDuplicateTestInNotVisible()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var canExecute = serviceTest.DuplicateTestCommand.CanExecute(null);
            Assert.IsFalse(canExecute);
        }

        [Then(@"Duplicate Test is ""(.*)""")]
        public void ThenDuplicateTestIs(string visibilityStatus)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var canExecute = serviceTest.DuplicateTestCommand.CanExecute(null);
            if (visibilityStatus == "Enabled")
            {
                Assert.IsTrue(canExecute);
            }
            else
            {
                Assert.IsFalse(canExecute);
            }
        }

        [Then(@"Duplicate Test in Visible")]
        public void ThenDuplicateTestInVisible()
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var canExecute = serviceTest.DuplicateTestCommand.CanExecute(null);
            Assert.IsTrue(canExecute);
        }


        [Then(@"The duplicate Name popup is shown")]
        public void ThenTheDuplicateNamePopupIsShown()
        {
            var mock = (Mock<Common.Interfaces.Studio.Controller.IPopupController>)MyContext["popupController"];
            mock.Verify(controller => controller.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.OK, MessageBoxImage.Error, null, false, true, false, false));
        }

        [Given(@"I have a folder ""(.*)""")]
        public void GivenIHaveAFolder(string foldername)
        {
            var category = ResourceCat + foldername;
            MyContext.Add("folderPath", category);
        }

        [Given(@"I have a resouce workflow ""(.*)"" inside Home")]
        public void GivenIHaveAResouceWorkflowInsideHome(string resourceName)
        {
            var path = MyContext.Get<string>("folderPath");
            var environmentModel = EnvironmentRepository.Instance.Source;


            var resourceId = Guid.NewGuid();
            // ReSharper disable once UnusedVariable
            var resourceModel = new ResourceModel(environmentModel)
            {
                ResourceName = resourceName,
                DisplayName = resourceName,
                DataList = "",
                ID = resourceId,
                Category = path + "\\" + resourceName
            };
            var workflowHelper = new WorkflowHelper();
            var builder = workflowHelper.CreateWorkflow(resourceName);
            resourceModel.WorkflowXaml = workflowHelper.GetXamlDefinition(builder);
            environmentModel.ResourceRepository.SaveToServer(resourceModel);
            ScenarioContext.Current.Add(resourceName + "id", resourceId);
        }

        [Given(@"I add ""(.*)"" to ""(.*)""")]
        public void GivenIAddTo(string testNames, string rName)
        {
            string path;
            MyContext.TryGetValue("folderPath", out path);
            var environmentModel = EnvironmentRepository.Instance.Source;
            var serviceTestModelTos = new List<IServiceTestModelTO>();
            environmentModel.ResourceRepository.ForceLoad();
            if (!string.IsNullOrEmpty(path))
            {

                var savedSource = environmentModel.ResourceRepository.All().First(model => model.Category.Equals(path + "\\" + rName, StringComparison.InvariantCultureIgnoreCase));
                MyContext[rName + "id"] = savedSource.ID;
                var resourceID = MyContext.Get<Guid>(rName + "id");
                lock (_syncRoot)
                {
                    var testNamesNames = testNames.Split(',');
                    foreach (var resourceName in testNamesNames)
                    {
                        Dev2JsonSerializer serializer = new Dev2JsonSerializer();
                        var serviceTestModelTO = serializer.Deserialize<ServiceTestModelTO>(Json);
                        serviceTestModelTO.TestName = resourceName;
                        serviceTestModelTO.ResourceId = resourceID;
                        serviceTestModelTO.Inputs = new List<IServiceTestInput>();
                        serviceTestModelTO.Outputs = new List<IServiceTestOutput>();
                        serviceTestModelTO.AuthenticationType = AuthenticationType.Windows;

                        serviceTestModelTos.Add(serviceTestModelTO);
                    }
                }

                // ReSharper disable once UnusedVariable
                var resourceModel = new ResourceModel(environmentModel)
                {
                    ID = savedSource.ID,
                    Category = path + "\\" + rName,
                    ResourceName = rName
                };
                var executeMessage = environmentModel.ResourceRepository.SaveTests(resourceModel, serviceTestModelTos);
                Assert.IsTrue(executeMessage.Result == SaveResult.Success || executeMessage.Result == SaveResult.ResourceUpdated);
            }
            else
            {
                var resourceId = new Guid("acb75027-ddeb-47d7-814e-a54c37247ec1");
                lock (_syncRoot)
                {
                    var testNamesNames = testNames.Split(',');
                    foreach (var resourceName in testNamesNames)
                    {
                        Dev2JsonSerializer serializer = new Dev2JsonSerializer();
                        var serviceTestModelTO = serializer.Deserialize<ServiceTestModelTO>(Json);
                        serviceTestModelTO.TestName = resourceName;

                        serviceTestModelTO.ResourceId = resourceId;
                        serviceTestModelTO.Inputs = new List<IServiceTestInput>();
                        serviceTestModelTO.Outputs = new List<IServiceTestOutput>();
                        serviceTestModelTO.AuthenticationType = AuthenticationType.Windows;

                        serviceTestModelTos.Add(serviceTestModelTO);
                    }
                }

                var testFrameworkFromContext = GetTestFrameworkFromContext();

                var executeMessage = environmentModel.ResourceRepository.SaveTests(testFrameworkFromContext.ResourceModel, serviceTestModelTos);
                Assert.IsTrue(executeMessage.Result == SaveResult.Success || executeMessage.Result == SaveResult.ResourceUpdated);
                testFrameworkFromContext = new ServiceTestViewModel(testFrameworkFromContext.ResourceModel, new SynchronousAsyncWorker(), new Mock<IEventAggregator>().Object, new SpecExternalProcessExecutor(), new Mock<IWorkflowDesignerViewModel>().Object);
                MyContext["testFramework"] = testFrameworkFromContext;

            }
        }
        [When(@"I delete folder ""(.*)""")]
        public void WhenIDeleteFolder(string folderName)
        {
            var path = MyContext.Get<string>("folderPath");
            var environmentModel = EnvironmentRepository.Instance.Source;

            var controller = new CommunicationController { ServiceName = "DeleteItemService" };
            controller.AddPayloadArgument("folderToDelete", path);
            var result = controller.ExecuteCommand<IExplorerRepositoryResult>(environmentModel.Connection, GlobalConstants.ServerWorkspaceID);
            if (result.Status != ExecStatus.Success)
            {
                throw new WarewolfSaveException(result.Message, null);
            }

        }

        private static Guid helloGuid;
        [Given(@"the test builder is open with existing service ""(.*)""")]
        public void GivenTheTestBuilderIsOpenWithExistingService(string workflowName)
        {
            var env = EnvironmentRepository.Instance.Source;
            env.ForceLoadResources();
            var res = env.ResourceRepository.FindSingle(model => model.ResourceName.Equals(workflowName, StringComparison.InvariantCultureIgnoreCase), true);
            var contextualResource = env.ResourceRepository.LoadContextualResourceModel(res.ID);
            helloGuid = res.ID;
            var serviceTestVm = new ServiceTestViewModel(contextualResource, new SynchronousAsyncWorker(), new Mock<IEventAggregator>().Object, new SpecExternalProcessExecutor(), new Mock<IWorkflowDesignerViewModel>().Object);
            Assert.IsNotNull(serviceTestVm);
            Assert.IsNotNull(serviceTestVm.ResourceModel);
            MyContext.Add("testFramework", serviceTestVm);
        }


        [Then(@"service debug inputs as")]
        public void ThenServiceDebugInputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            Assert.IsNotNull(test);
            Assert.IsNotNull(test.DebugForTest);
            var debugStates = test.DebugForTest;
            var serviceStartDebug = debugStates[0];
            foreach (var tableRow in table.Rows)
            {
                var variableName = tableRow["Variable"];
                var variableValue = tableRow["Value"];
                IDebugItemResult debugItemResult = null;
                var debugItem = serviceStartDebug.Inputs.FirstOrDefault(item =>
                {
                    debugItemResult = item.ResultsList.FirstOrDefault(result => result.Variable.Equals(variableName, StringComparison.InvariantCultureIgnoreCase));
                    return debugItemResult != null;
                });
                Assert.IsNotNull(debugItem);
                Assert.IsNotNull(debugItemResult);
                Assert.AreEqual(variableValue, debugItemResult.Value);
            }
        }

        [Then(@"the service debug outputs as")]
        public void ThenTheServiceDebugOutputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            Assert.IsNotNull(test);
            Assert.IsNotNull(test.DebugForTest);
            var debugStates = test.DebugForTest;
            var serviceEndDebug = debugStates[debugStates.Count - 1];
            foreach (var tableRow in table.Rows)
            {
                var variableName = tableRow["Variable"];
                var variableValue = tableRow["Value"];
                IDebugItemResult debugItemResult = null;
                var debugItem = serviceEndDebug.Outputs.FirstOrDefault(item =>
                {
                    debugItemResult = item.ResultsList.FirstOrDefault(result => result.Variable.Equals(variableName, StringComparison.InvariantCultureIgnoreCase));
                    return debugItemResult != null;
                });
                Assert.IsNotNull(debugItem);
                Assert.IsNotNull(debugItemResult);
                Assert.AreEqual(variableValue, debugItemResult.Value);
            }
        }

        [Then(@"I add mock steps as")]
        public void ThenIAddMockStepsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            WorkflowHelper helper = new WorkflowHelper();
            var builder = helper.ReadXamlDefinition(serviceTest.ResourceModel.WorkflowXaml);
            Assert.IsNotNull(builder);
            var act = (Flowchart)builder.Implementation;
            foreach (var tableRow in table.Rows)
            {
                var actNameToFind = tableRow["Step Name"];
                var actType = tableRow["Activity Type"];
                if (actNameToFind != null)
                {
                    if (string.Equals(actType, "Decision", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var foundNode = act.Nodes.FirstOrDefault(node =>
                        {
                            var searchNode = node as FlowDecision;
                            if (searchNode != null)
                            {
                                return searchNode.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                            }
                            return false;
                        });
                        if (foundNode != null)
                        {
                            var decisionNode = foundNode as FlowDecision;
                            // ReSharper disable once PossibleNullReferenceException
                            var condition = decisionNode.Condition;
                            var activity = (DsfFlowNodeActivity<bool>)condition;
                            var expression = activity.ExpressionText;
                            if (expression != null)
                            {
                                var eval = Dev2DecisionStack.ExtractModelFromWorkflowPersistedData(expression);

                                if (!string.IsNullOrEmpty(eval))
                                {
                                    Dev2JsonSerializer ser = new Dev2JsonSerializer();
                                    var dds = ser.Deserialize<Dev2DecisionStack>(eval);
                                    var armToUse = tableRow["Output Value"];

                                    if (dds.FalseArmText.Equals(armToUse, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        var serviceTestOutputs = new ObservableCollection<IServiceTestOutput> { new ServiceTestOutput(GlobalConstants.ArmResultText, dds.FalseArmText, "", "") };
                                        test.AddTestStep(activity.UniqueID, activity.DisplayName, typeof(DsfDecision).Name, serviceTestOutputs, StepType.Mock);
                                    }
                                    else if (dds.TrueArmText.Equals(armToUse, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        var serviceTestOutputs = new ObservableCollection<IServiceTestOutput> { new ServiceTestOutput(GlobalConstants.ArmResultText, dds.TrueArmText, "", "") };
                                        test.AddTestStep(activity.UniqueID, activity.DisplayName, typeof(DsfDecision).Name, serviceTestOutputs, StepType.Mock);
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        var foundNode = act.Nodes.FirstOrDefault(node =>
                        {
                            var searchNode = node as FlowStep;
                            if (searchNode != null)
                            {
                                return searchNode.Action.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                            }
                            return false;
                        });
                        var decisionNode = foundNode as FlowStep;
                        // ReSharper disable once PossibleNullReferenceException
                        var action = decisionNode.Action;
                        var activity = (DsfActivityAbstract<string>)action;
                        var var = tableRow["Output Variable"];
                        var value = tableRow["Output Value"];
                        var from = tableRow["Output From"];
                        var to = tableRow["Output To"];
                        var serviceTestOutputs = new ObservableCollection<IServiceTestOutput> { new ServiceTestOutput(var, value, from, to) };
                        var type = activity.GetType();
                        test.AddTestStep(activity.UniqueID, activity.DisplayName, type.Name, serviceTestOutputs, StepType.Mock);
                    }
                }
            }
        }

        [Then(@"I Add all TestSteps")]
        public void ThenIAddAllTestSteps()
        {

            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            WorkflowHelper helper = new WorkflowHelper();
            var builder = helper.ReadXamlDefinition(serviceTest.ResourceModel.WorkflowXaml);
            Assert.IsNotNull(builder);
            var act = (Flowchart)builder.Implementation;
            foreach (var flowNode in act.Nodes)
            {
                var modelItem = ModelItemUtils.CreateModelItem(flowNode);
                var methodInfo = typeof(ServiceTestViewModel).GetMethod("ItemSelectedAction", BindingFlags.Instance | BindingFlags.NonPublic);
                methodInfo.Invoke(serviceTest, new object[] { modelItem });
            }
        }

        [Then(@"I Add ""(.*)"" as TestStep")]
        public void ThenIAddAsTestStep(string actNameToFind)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            WorkflowHelper helper = new WorkflowHelper();
            var builder = helper.ReadXamlDefinition(serviceTest.ResourceModel.WorkflowXaml);
            Assert.IsNotNull(builder);
            var act = (Flowchart)builder.Implementation;
            var actStartNode = act.StartNode;
            if (act.Nodes.Count == 0 && actStartNode != null)
            {
                var searchNode = actStartNode as FlowStep;
                while (searchNode != null)
                {
                    var isCorr =  searchNode.Action.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                    if (isCorr)
                    {
                        var modelItem = ModelItemUtils.CreateModelItem(searchNode.Action);
                        var methodInfo = typeof(ServiceTestViewModel).GetMethod("ItemSelectedAction", BindingFlags.Instance | BindingFlags.NonPublic);
                        methodInfo.Invoke(serviceTest, new object[] { modelItem });
                        searchNode = null;
                    }
                    else
                    {
                        searchNode = searchNode.Next as FlowStep;
                    }
                }
            }
            else
            {
                foreach (var flowNode in act.Nodes)
                {
                    var searchNode = flowNode as FlowStep;
                    var isCorr = searchNode != null && searchNode.Action.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                    if (isCorr)
                    {
                        var modelItem = ModelItemUtils.CreateModelItem(flowNode);
                        var methodInfo = typeof(ServiceTestViewModel).GetMethod("ItemSelectedAction", BindingFlags.Instance | BindingFlags.NonPublic);
                        methodInfo.Invoke(serviceTest, new object[] { modelItem });
                        break;
                    }
                }
            }
        }

        [Then(@"I Add all ""(.*)"" as TestStep")]
        public void ThenIAddAllAsTestStep(string actNameToFind)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            WorkflowHelper helper = new WorkflowHelper();
            var builder = helper.ReadXamlDefinition(serviceTest.ResourceModel.WorkflowXaml);
            Assert.IsNotNull(builder);
            var act = (Flowchart)builder.Implementation;
            foreach (var flowNode in act.Nodes)
            {
                var searchNode = flowNode as FlowStep;
                var isCorr = searchNode != null && searchNode.Action.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                if (isCorr)
                {
                    var modelItem = ModelItemUtils.CreateModelItem(flowNode);
                    var methodInfo = typeof(ServiceTestViewModel).GetMethod("ItemSelectedAction", BindingFlags.Instance | BindingFlags.NonPublic);
                    methodInfo.Invoke(serviceTest, new object[] { modelItem });
                }
            }
        }

        [Then(@"I add StepOutputs as")]
        public void ThenIAddStepOutputsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();

            foreach (var tableRow in table.Rows)
            {
                var index = Convert.ToInt32(tableRow["stepIndex"]);
                var childIndex = Convert.ToInt32(tableRow["ChildIndex"]);
                var varName = tableRow["Variable Name"];
                var condition = tableRow["Condition"];
                var value = tableRow["Value"];
                var serviceTestStep = serviceTest.SelectedServiceTest.TestSteps[index];
                if (childIndex < serviceTestStep.Children.Count)
                    if (serviceTestStep.Children != null)
                    {
                        serviceTestStep.Children[childIndex].StepOutputs = new ObservableCollection<IServiceTestOutput>
                        {
                            new ServiceTestOutput(varName, value, "", "")
                            {
                                AssertOp = condition,
                            }
                        };

                    }
            }
        }




        [Then(@"I add Assert steps as")]
        public void ThenIAddAssertStepsAs(Table table)
        {
            ServiceTestViewModel serviceTest = GetTestFrameworkFromContext();
            var test = serviceTest.SelectedServiceTest;
            WorkflowHelper helper = new WorkflowHelper();
            var builder = helper.ReadXamlDefinition(serviceTest.ResourceModel.WorkflowXaml);
            Assert.IsNotNull(builder);
            var act = (Flowchart)builder.Implementation;
            foreach (var tableRow in table.Rows)
            {
                var actNameToFind = tableRow["Step Name"];
                var actType = tableRow["Activity Type"];
                if (actNameToFind != null)
                {
                    if (string.Equals(actType, "Decision", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var foundNode = act.Nodes.FirstOrDefault(node =>
                        {
                            var searchNode = node as FlowDecision;
                            if (searchNode != null)
                            {
                                return searchNode.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                            }
                            return false;
                        });
                        if (foundNode != null)
                        {
                            var decisionNode = foundNode as FlowDecision;
                            // ReSharper disable once PossibleNullReferenceException
                            var condition = decisionNode.Condition;
                            var activity = (Unlimited.Applications.BusinessDesignStudio.Activities.DsfFlowNodeActivity<bool>)condition;
                            var expression = activity.ExpressionText;
                            if (expression != null)
                            {
                                var eval = Dev2DecisionStack.ExtractModelFromWorkflowPersistedData(expression);

                                if (!string.IsNullOrEmpty(eval))
                                {
                                    Dev2JsonSerializer ser = new Dev2JsonSerializer();
                                    var dds = ser.Deserialize<Dev2DecisionStack>(eval);
                                    var armToUse = tableRow["Output Value"];
                                    test.AddTestStep(activity.UniqueID, activity.DisplayName, typeof(DsfDecision).Name, new ObservableCollection<IServiceTestOutput>() { new ServiceTestOutput(GlobalConstants.ArmResultText, armToUse, "", "") });

                                }
                            }

                        }
                    }
                    else
                    {
                        var foundNode = act.Nodes.FirstOrDefault(node =>
                        {
                            var searchNode = node as FlowStep;
                            if (searchNode != null)
                            {
                                return searchNode.Action.DisplayName.TrimEnd(' ').Equals(actNameToFind, StringComparison.InvariantCultureIgnoreCase);
                            }
                            return false;
                        });
                        var decisionNode = foundNode as FlowStep;
                        // ReSharper disable once PossibleNullReferenceException
                        var action = decisionNode.Action;
                        var activity = (Unlimited.Applications.BusinessDesignStudio.Activities.DsfActivityAbstract<string>)action;
                        var var = tableRow["Output Variable"];
                        var value = tableRow["Output Value"];
                        var from = tableRow["Output From"];
                        var to = tableRow["Output To"];
                        var serviceTestOutputs = new ObservableCollection<IServiceTestOutput> { new ServiceTestOutput(var, value, from, to) };
                        var type = activity.GetType();
                        test.AddTestStep(activity.UniqueID, activity.DisplayName, type.Name, serviceTestOutputs);
                    }
                }
            }
        }



        private IEnumerable<IServiceTestModel> GetTestForCurrentTestFramework()
        {
            var testFrameworkFromContext = GetTestFrameworkFromContext();
            var serviceTestModels = testFrameworkFromContext.Tests.Where(model => model.GetType() != typeof(DummyServiceTest));
            return serviceTestModels;
        }

        ServiceTestViewModel GetTestFrameworkFromContext()
        {
            ServiceTestViewModel serviceTest;
            if (MyContext.TryGetValue("testFramework", out serviceTest))
            {
                return serviceTest;
            }
            Assert.Fail("Test Framework ViewModel not found");
            return null;
        }

        [AfterScenario("TestFramework")]
        public void CleanupTestFramework()
        {
            ServiceTestViewModel serviceTest;
            if (MyContext.TryGetValue("testFramework", out serviceTest))
            {
                serviceTest?.Dispose();
            }
        }

    }
}
