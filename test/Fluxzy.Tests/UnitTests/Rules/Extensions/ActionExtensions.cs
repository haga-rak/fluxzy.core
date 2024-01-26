using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients.Mock;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Extensions;
using Fluxzy.Rules.Filters.RequestFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules.Extensions
{
    public class ActionExtensions
    {
        private readonly FluxzySetting _setting;

        public ActionExtensions()
        {
            _setting = FluxzySetting.CreateDefault();
            _setting.ClearAlterationRules();
        }

        [Fact]
        public void ValidateAbort()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .Abort();

            Assert.Single(_setting.AlterationRules); 
            Assert.Equal(typeof(AbortAction), _setting.AlterationRules.First().Action.GetType());
        }

        [Fact]
        public void ValidateForward()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .Forward("https://google.com");

            Assert.Single(_setting.AlterationRules); 
            Assert.Equal(typeof(ForwardAction), _setting.AlterationRules.First().Action.GetType());
        }

        [Fact]
        public void ValidateDo()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .Do(new AddBasicAuthenticationAction("leeloo", "multipass"));

            Assert.Single(_setting.AlterationRules); 
            Assert.Equal(typeof(AddBasicAuthenticationAction), _setting.AlterationRules.First().Action.GetType());
        }

        [Fact]
        public void ValidateReplyByteArray()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .ReplyByteArray(new byte[4]);

            var action = _setting.AlterationRules.First().Action as MockedResponseAction;

            Assert.Single(_setting.AlterationRules); 
            Assert.NotNull(action);
            Assert.NotNull(action.Response.Body);
            Assert.Equal(BodyContentLoadingType.FromImmediateArray, action.Response.Body.Origin);
        }

        [Fact]
        public void ValidateReplyText()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .ReplyText("yes");

            var action = _setting.AlterationRules.First().Action as MockedResponseAction;

            Assert.Single(_setting.AlterationRules); 
            Assert.NotNull(action);
            Assert.NotNull(action.Response.Body);
            Assert.Equal(BodyContentLoadingType.FromString, action.Response.Body.Origin);
        }

        [Fact]
        public void ValidateReplyJson()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .ReplyJson("{ yo : 5 }");

            var action = _setting.AlterationRules.First().Action as MockedResponseAction;

            Assert.Single(_setting.AlterationRules); 
            Assert.NotNull(action);
            Assert.NotNull(action.Response.Body);
            Assert.Equal(BodyContentLoadingType.FromString, action.Response.Body.Origin);
        }

        [Fact]
        public void ReplyJsonFile()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .ReplyJsonFile("filename.txt");

            var action = _setting.AlterationRules.First().Action as MockedResponseAction;

            Assert.Single(_setting.AlterationRules); 
            Assert.NotNull(action);
            Assert.NotNull(action.Response.Body);
            Assert.Equal(BodyContentLoadingType.FromFile, action.Response.Body.Origin);
        }

        [Fact]
        public void ReplyFile()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .ReplyFile("filename.txt");

            var action = _setting.AlterationRules.First().Action as MockedResponseAction;

            Assert.Single(_setting.AlterationRules); 
            Assert.NotNull(action);
            Assert.NotNull(action.Response.Body);
            Assert.Equal(BodyContentLoadingType.FromFile, action.Response.Body.Origin);
        }

        [Theory]
        [MemberData(nameof(GetValidateParams))]
        public void Validate(string typeName, 
            Func<IConfigureActionBuilder, IConfigureFilterBuilder> validation)
        {
            validation(_setting.ConfigureRule()
                              .WhenHostMatch("fakehost.com"));

            Assert.Single(_setting.AlterationRules);
            Assert.Equal(typeName, _setting.AlterationRules.First().Action.GetType().FullName);
        }

        public static IEnumerable<object[]> GetValidateParams()
        {
            yield return new object[] {
                typeof(DeleteRequestHeaderAction).FullName!,
                new Func<IConfigureActionBuilder, IConfigureFilterBuilder>
                    (builder => builder.DeleteRequestHeader("a"))
            };

            yield return new object[] {
                typeof(DeleteResponseHeaderAction).FullName!,
                new Func<IConfigureActionBuilder, IConfigureFilterBuilder>
                    (builder => builder.DeleteResponseHeader("a"))
            };

            yield return new object[] {
                typeof(AddRequestHeaderAction).FullName!,
                new Func<IConfigureActionBuilder, IConfigureFilterBuilder>
                    (builder => builder.AddRequestHeader("a", "b"))
            };

            yield return new object[] {
                typeof(AddResponseHeaderAction).FullName!,
                new Func<IConfigureActionBuilder, IConfigureFilterBuilder>
                    (builder => builder.AddResponseHeader("a", "b"))
            };

            yield return new object[] {
                typeof(UpdateRequestHeaderAction).FullName!,
                new Func<IConfigureActionBuilder, IConfigureFilterBuilder>
                    (builder => builder.UpdateRequestHeader("a", "b"))
            };

            yield return new object[] {
                typeof(UpdateResponseHeaderAction).FullName!,
                new Func<IConfigureActionBuilder, IConfigureFilterBuilder>
                    (builder => builder.UpdateResponseHeader("a", "b"))
            };
        }
    }
}
 