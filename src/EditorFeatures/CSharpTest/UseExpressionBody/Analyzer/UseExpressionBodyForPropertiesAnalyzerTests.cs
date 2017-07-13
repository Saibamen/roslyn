﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForPropertiesAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyDiagnosticAnalyzer(), new UseExpressionBodyCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> UseBlockBody =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> UseBlockBodyExceptAccessor =>
            OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }
    }
}",
@"class C
{
    int Foo => Bar();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithSetter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|return|] Bar();
        }

        set
        {
        }
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        [A]
        get
        {
            [|return|] Bar();
        }
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingOnSetter1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        set
        {
            [|Bar|]();
        }
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|throw|] new NotImplementedException();
        }
    }
}",
@"class C
{
    int Foo => throw new NotImplementedException();
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo
    {
        get
        {
            [|throw|] new NotImplementedException(); // comment
        }
    }
}",
@"class C
{
    int Foo => throw new NotImplementedException(); // comment
}", ignoreTrivia: false, options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo [|=>|] Bar();
}",
@"class C
{
    int Foo
    {
        get
        {
            return Bar();
        }
    }
}", options: UseBlockBody);
        }

        [WorkItem(20363, "https://github.com/dotnet/roslyn/issues/20363")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBodyForAccessorEventWhenAccessorWantExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo [|=>|] Bar();
}",
@"class C
{
    int Foo
    {
        get { return Bar(); }
    }
}", options: UseBlockBodyExceptAccessor);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo [|=>|] throw new NotImplementedException();
}",
@"class C
{
    int Foo
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseBlockBody4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo [|=>|] throw new NotImplementedException(); // comment
}",
@"class C
{
    int Foo
    {
        get
        {
            throw new NotImplementedException(); // comment
        }
    }
}", ignoreTrivia: false, options: UseBlockBody);
        }

        [WorkItem(16386, "https://github.com/dotnet/roslyn/issues/16386")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUseExpressionBodyKeepTrailingTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private string _prop = ""HELLO THERE!"";
    public string Prop { get { [|return|] _prop; } }

    public string OtherThing => ""Pickles"";
}",
@"class C
{
    private string _prop = ""HELLO THERE!"";
    public string Prop => _prop;

    public string OtherThing => ""Pickles"";
}", ignoreTrivia: false, options: UseExpressionBody);
        }

        [WorkItem(19235, "https://github.com/dotnet/roslyn/issues/19235")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectivesInBlockBody1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo
    {
        get
        {
#if true
            [|return|] Bar();
#else
            return Baz();
#endif
        }
    }
}",

@"class C
{
    int Foo =>
#if true
            Bar();
#else
            return Baz();
#endif

}", ignoreTrivia: false,
    parameters: new TestParameters(options: UseExpressionBody));
        }

        [WorkItem(19235, "https://github.com/dotnet/roslyn/issues/19235")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestDirectivesInBlockBody2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo
    {
        get
        {
#if false
            return Bar();
#else
            [|return|] Baz();
#endif
        }
    }
}",

@"class C
{
    int Foo =>
#if false
            return Bar();
#else
            Baz();
#endif

}", ignoreTrivia: false,
    parameters: new TestParameters(options: UseExpressionBody));
        }

        [WorkItem(19235, "https://github.com/dotnet/roslyn/issues/19235")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithDirectivesInExpressionBody1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Foo [|=>|]
#if true
            Bar();
#else
            Baz();
#endif
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [WorkItem(19235, "https://github.com/dotnet/roslyn/issues/19235")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWithDirectivesInExpressionBody2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int Foo [|=>|]
#if false
            Bar();
#else
            Baz();
#endif
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [WorkItem(19193, "https://github.com/dotnet/roslyn/issues/19193")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMoveTriviaFromExpressionToReturnStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int Foo(int i) [|=>|]
        //comment
        i * i;
}",
@"class C
{
    int Foo(int i)
    {
        //comment
        return i * i;
    }
}", ignoreTrivia: false,
    options: UseBlockBody);
        }
    }
}
