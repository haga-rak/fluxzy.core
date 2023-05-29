// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Tests.Variables
{
    public class VariableEvaluationHelperTests
    {
        public VariableEvaluationHelperTests()
        {
            Environment.SetEnvironmentVariable("mytestvar", "glouglou");
        }

        //[Fact]
        //public void Push_And_Get_Filter()
        //{
        //    AbsoluteUriFilter filter = new("http://${env.mytestvar}");

        //    VariableEvaluationHelper.Inject(filter, new(), null, null); 

        //    Assert.Equal("http://glouglou", filter.Pattern);
        //}

        //[Fact]
        //public void Push_And_Get_Action()
        //{
        //    var action = new DeleteRequestHeaderAction("MyPreferred${env.mytestvar}"); 

        //    VariableEvaluationHelper.Inject(action, new(), null, null); 

        //    Assert.Equal("MyPreferredglouglou", action.HeaderName);
        //}

        //[Fact]
        //public void Push_And_Get_Action_With_Null()
        //{
        //    var action = new DeleteRequestHeaderAction(null!);

        //    VariableEvaluationHelper.Inject(action, new(), null, null);  

        //    Assert.Equal(null, action.HeaderName);
        //}
    }
}
