﻿using System;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dev2.Activities.Designers2.Web_Service_Post;
using Dev2.Activities.Designers2.Web_Service_Put;
using Dev2.Common.Interfaces;
using Dev2.Common.Interfaces.Core;
using Dev2.Common.Interfaces.DB;
using Dev2.Common.Interfaces.ServerProxyLayer;
using Dev2.Common.Interfaces.WebService;
using Dev2.Common.Interfaces.WebServices;
using Dev2.Communication;
using Dev2.Runtime.ServiceModel.Data;
using Dev2.Studio.Core.Activities.Utils;
using Dev2.Studio.Core.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TechTalk.SpecFlow;
namespace Dev2.Activities.Specs.Toolbox.Resources.Web
{
    [Binding]
    public sealed class PutWebConnectorToolSteps
    {
        private WebServiceSourceDefinition _dev2CountriesWebServiceWebSource;
        private WebServiceSourceDefinition _webHelooWebSource;
        private WebServiceSourceDefinition _googleWebSource;

        [Given(@"I drag Web Put Request Connector Tool onto the design surface")]
        public void GivenIDragWebPutRequestConnectorOnToTheDesignSurface()
        {
            var webPutActivity = new DsfWebPutActivity();
            ModelItem modelItem = ModelItemUtils.CreateModelItem(webPutActivity);
            var mockWebServiceInputViewModel = new Mock<IManageWebServiceInputViewModel>();
            Mock<IWebServiceModel> mockServiceModel = new Mock<IWebServiceModel>();
            var mockEnvironmentRepository = new Mock<IEnvironmentRepository>();
            var mockEnvironmentModel = new Mock<IEnvironmentModel>();
            mockEnvironmentModel.Setup(model => model.IsConnected).Returns(true);
            mockEnvironmentModel.Setup(model => model.IsLocalHost).Returns(true);
            mockEnvironmentModel.Setup(model => model.ID).Returns(Guid.Empty);
            mockEnvironmentModel.Setup(model => model.IsLocalHostCheck()).Returns(false);
            mockEnvironmentRepository.Setup(repository => repository.ActiveEnvironment).Returns(mockEnvironmentModel.Object);
            mockEnvironmentRepository.Setup(repository => repository.FindSingle(It.IsAny<Expression<Func<IEnvironmentModel, bool>>>())).Returns(mockEnvironmentModel.Object);

            _webHelooWebSource = new WebServiceSourceDefinition()
            {
                Name = "Web Hello"
            };

            _dev2CountriesWebServiceWebSource = new WebServiceSourceDefinition()
            {
                Name = "Dev2CountriesWebSource",
                HostName = "http://rsaklfsvrtfsbld/integrationTestSite/GetCountries.ashx"
            };
            _googleWebSource = new WebServiceSourceDefinition()
            {
                Name = "Google Address LookUp"
            };

            var webServiceSources = new ObservableCollection<IWebServiceSource>() { _dev2CountriesWebServiceWebSource };
            mockServiceModel.Setup(model => model.RetrieveSources()).Returns(webServiceSources);
            mockServiceModel.Setup(model => model.EditSource(It.IsAny<IWebServiceSource>())).Verifiable();
            mockWebServiceInputViewModel.SetupAllProperties();

            var putViewModel = new WebServicePutViewModel(modelItem, mockServiceModel.Object);
            ScenarioContext.Current.Add("viewModel",putViewModel);
            ScenarioContext.Current.Add("mockWebServiceInputViewModel", mockWebServiceInputViewModel);
            ScenarioContext.Current.Add("mockServiceModel", mockServiceModel);

        }

        private static WebServicePutViewModel GetViewModel()
        {
            return ScenarioContext.Current.Get<WebServicePutViewModel>("viewModel");
        }

        private static Mock<IWebServiceModel> GetServiceModel()
        {
            return ScenarioContext.Current.Get<Mock<IWebServiceModel>>("mockServiceModel");
        }
        [When(@"Put Test Input Is Succesfull")]
        public void WhenTestInputIsSuccesful()
        {
            GetViewModel().ManageServiceInputViewModel.TestCommand.Execute(null);
        }

        [Given(@"Put New Is Enabled")]
        [When(@"Put New Is Enabled")]
        [Then(@"Put New Is Enabled")]
        public void WhenPutNewIsEnabled()
        {
            var canExecute = GetViewModel().SourceRegion.NewSourceCommand.CanExecute(null);
            Assert.IsTrue(canExecute);
        }

        [Given(@"Put Edit Is Enabled")]
        [When(@"Put Edit Is Enabled")]
        [Then(@"Put Edit Is Enabled")]
        public void WhenPutEditIsEnabled()
        {
            var canExecute = GetViewModel().SourceRegion.EditSourceCommand.CanExecute(null);
            Assert.IsTrue(canExecute);
        }

         [Given(@"Put Edit is Disabled")]
        public void GivenPutEditIsDisabled()
        {
            var canExecuteNewCommand = GetViewModel().SourceRegion.EditSourceCommand.CanExecute(null);
            Assert.IsFalse(canExecuteNewCommand);
        }

        [When(@"When I Click Put Edit")]
        public void WhenIClickPutEdit()
        {
            GetViewModel().SourceRegion.EditSourceCommand.Execute(null);
        }

        [Then(@"Put Header Is Enabled")]
        public void ThenPutHeaderIsEnabled()
        {
            Assert.AreEqual(1,GetViewModel().InputArea.Headers.Count);
        }

        [Then(@"Put HeaderAppears As")]
        public void ThenPutHeaderAppearsAs(Table table)
        {
            var headers = GetViewModel().InputArea.Headers;
            foreach(var tableRow in table.Rows)
            {
                var name = tableRow["Header"];
                var value = tableRow["Value"];
                var found = headers.FirstOrDefault(nameValue => nameValue.Name == name && nameValue.Value == value);
                Assert.IsNotNull(found);
            }
        }

        [Then(@"Put Body Is Enabled")]
        public void ThenPutBodyIsEnabled()
        {
            Assert.IsTrue(GetViewModel().InputArea.IsVisible);
        }

        [Then(@"Put Url is Visible")]
        public void ThenPutUrlIsVisible()
        {
            Assert.IsTrue(GetViewModel().InputArea.IsVisible);
        }
        
        [Then(@"Put Query is Enabled")]
        public void ThenPutQueryIsEnabled()
        {
            Assert.IsTrue(GetViewModel().InputArea.IsVisible);
        }

        [Then(@"Put Generate Outputs is Enabled")]
        public void ThenPutGenerateOutputsIsEnabled()
        {
            var canGenerateOutputs = GetViewModel().TestInputCommand.CanExecute();
            Assert.IsTrue(canGenerateOutputs);
        }

        [Then(@"Put the Generate Outputs window is shown")]
        public void ThenThePutGenerateOutputsWindowIsShown()
        {
            var servicePutViewModel = GetViewModel();
            Assert.IsTrue(servicePutViewModel.GenerateOutputsVisible);
            Assert.IsTrue(servicePutViewModel.ManageServiceInputViewModel.InputArea.IsVisible);
            Assert.IsFalse(servicePutViewModel.ManageServiceInputViewModel.OutputArea.IsVisible);
            Assert.IsTrue(servicePutViewModel.ManageServiceInputViewModel.IsVisible);
        }
          [Then(@"Put Variables are Enabled")]
        public void ThenPutVariablesAreEnabled()
        {
            var servicePutViewModel = GetViewModel();
            Assert.IsTrue(servicePutViewModel.ManageServiceInputViewModel.InputArea.IsVisible);
        }

        [Then(@"Put the response is loaded")]
        public void ThenPutTheResponseIsLoaded()
        {
            var putViewModel = GetViewModel();
            Assert.IsTrue(putViewModel.ManageServiceInputViewModel.OutputArea.IsVisible);
        }


        [Then(@"Put Mapping is Enabled")]
        public void ThenPutMappingIsEnabled()
        {
            var putViewModel = GetViewModel();
            Assert.IsTrue(putViewModel.OutputsRegion.IsVisible);
        }

         [Then(@"I enter ""(.*)"" as Put Query String")]
        public void ThenIEnterAsPutQueryString(string queryString)
        {
            var putViewModel = GetViewModel();
            putViewModel.InputArea.QueryString = queryString;
        }

        [Then(@"Put Url as ""(.*)""")]
        public void ThenPutUrlAs(string url)
        {
            var putViewModel = GetViewModel();
            Assert.AreEqual(url,putViewModel.InputArea.RequestUrl);
        }
        
        [Then(@"I add Put Header as")]
        public void ThenIAddPutHeaderAs(Table table)
        {
            var headers = GetViewModel().InputArea.Headers;
            foreach(var tableRow in table.Rows)
            {
                var name = tableRow["Name"];
                var value = tableRow["Value"];
                headers.Add(new NameValue(name,value));
            }
        }

        [Then(@"Put Input variables are")]
        public void ThenPutInputVariablesAre(Table table)
        {
            var serviceInputs = GetViewModel().ManageServiceInputViewModel.InputArea.Inputs;
            foreach(var tableRow in table.Rows)
            {
                var inputName = tableRow["Name"];
                var found = serviceInputs.FirstOrDefault(input => input.Name == inputName);
                Assert.IsNotNull(found);
            }
        }



         [Then(@"Put Test is Enabled")]
        public void ThenPutTestIsEnabled()
        {
            var putViewModel = GetViewModel();
            var canExecuteTest = putViewModel.ManageServiceInputViewModel.TestCommand.CanExecute(null);
            Assert.IsTrue(canExecuteTest);
        }
        
        [Then(@"Put Paste is Enabled")]
        public void ThenPutPasteIsEnabled()
        {
            var servicePutViewModel = GetViewModel();
            var canPaste = servicePutViewModel.ManageServiceInputViewModel.PasteResponseCommand.CanExecute(null);
            Assert.IsTrue(canPaste);
        }
        
        [Then(@"the ""(.*)"" Put Source tab is opened")]
        public void ThenThePutSourceTabIsOpened(string p0)
        {
            GetServiceModel().Verify(model => model.EditSource(It.IsAny<IWebServiceSource>()));
        }
        
        [Given(@"I click Put Generate Outputs")]
        [When(@"I click Put Generate Outputs")]
        [Then(@"I click Put Generate Outputs")]
        public async Task ThenIClickPutGenerateOutputs()
        {
            var servicePutViewModel = GetViewModel();
            await servicePutViewModel.TestInputCommand.Execute();
        }
        
        [Then(@"Put Response appears as ""(.*)""")]
        public void ThenPutResponseAppearsAs(string response)
        {
            var servicePutViewModel = GetViewModel();
            Assert.AreEqual(response,servicePutViewModel.ManageServiceInputViewModel.TestResults);
        }
        
        [Then(@"Put Mappings is Disabled")]
        public void ThenPutMappingsIsDisabled()
        {
            var putViewModel = GetViewModel();
            Assert.IsFalse(putViewModel.OutputsRegion.IsVisible);
        }
                      
        
        [Given(@"I click Put Done")]
        [When(@"I click Put Done")]
        [Then(@"I click Put Done")]
        public void ThenIClickPutDone()
        {
            var servicePutViewModel = GetViewModel();
            servicePutViewModel.ManageServiceInputViewModel.OkCommand.Execute(null);
        }
        
        [Then(@"Put output mappings are")]
        public void ThenPutOutputMappingsAre(Table table)
        {
            var servicePutViewModel = GetViewModel();
            var outputs = servicePutViewModel.OutputsRegion.Outputs;
            foreach(var tableRow in table.Rows)
            {
                var mappedFrom = tableRow["Mapped From"];
                var mappedTo = tableRow["Mapped To"];
                var found = outputs.FirstOrDefault(mapping => mapping.MappedFrom == mappedFrom && mapping.MappedTo == mappedTo);
                Assert.IsNotNull(found);
            }
        }

        [When(@"I Select ""(.*)"" as a Put web Source")]
        public void WhenISelectAsAPutWebSource(string sourceName)
        {
            if (sourceName == "Dev2CountriesWebService")
            {
                var serviceModel = GetServiceModel();
                var webService = new WebService
                {
                    RequestResponse = "{\"CountryID\" : \"a\",\"Description\":\"a\"}",
                    Recordsets = new RecordsetList
                {
                    new Runtime.ServiceModel.Data.Recordset
                    {
                        Name = "",
                        Fields = new List<RecordsetField>
                        {
                            new RecordsetField
                            {
                                Alias = "CountryID",
                                Name = "CountryID",
                                RecordsetAlias = ""
                            },
                            new RecordsetField
                            {
                                Alias = "Description",
                                Name = "Description",
                                RecordsetAlias = ""
                            }
                        }
                    }
                }
                };
                var serializer = new Dev2JsonSerializer();
                var testResult = serializer.Serialize(webService);
                serviceModel.Setup(model => model.TestService(It.IsAny<IWebService>())).Returns(testResult);
                GetViewModel().SourceRegion.SelectedSource = _dev2CountriesWebServiceWebSource;
            }
            else if (sourceName == "Google Address Lookup")
            {
                GetViewModel().SourceRegion.SelectedSource = _googleWebSource;
            }
            else
            {
                var serviceModel = GetServiceModel();
                var webService = new WebService
                {
                    RequestResponse = "{\"rec\" : [{\"a\":\"1\",\"b\":\"a\"}]}",
                    Recordsets = new RecordsetList
                {
                    new Runtime.ServiceModel.Data.Recordset
                    {
                        Name = "rec",
                        Fields = new List<RecordsetField>
                        {
                            new RecordsetField
                            {
                                Alias = "a",
                                Name = "a",
                                RecordsetAlias = "rec"
                            },
                            new RecordsetField
                            {
                                Alias = "b",
                                Name = "b",
                                RecordsetAlias = "rec"
                            }
                        }
                    }
                }
                };
                var serializer = new Dev2JsonSerializer();
                var testResult = serializer.Serialize(webService);
                serviceModel.Setup(model => model.TestService(It.IsAny<IWebService>())).Returns(testResult);
                GetViewModel().SourceRegion.SelectedSource = _webHelooWebSource;
            }
        }

        [Then(@"Put mapped outputs are")]
        public void ThenPutMappedOutputsAre(Table table)
        {
            var vm = GetViewModel();
            if (table.Rows.Count == 0)
            {
                if (vm.OutputsRegion.Outputs != null)
                    Assert.AreEqual(vm.OutputsRegion.Outputs.Count, 0);
            }
            else
            {
                var matched = table.Rows.Zip(vm.OutputsRegion.Outputs, (a, b) => new Tuple<TableRow, IServiceOutputMapping>(a, b));
                foreach (var a in matched)
                {
                    Assert.AreEqual(a.Item1[0], a.Item2.MappedFrom);
                    Assert.AreEqual(a.Item1[1], a.Item2.MappedTo);

                }
            }

    }
}
}

