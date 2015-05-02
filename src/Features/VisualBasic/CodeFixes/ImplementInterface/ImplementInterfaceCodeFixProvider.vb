' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ImplementInterface
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ImplementInterface

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ImplementInterface), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementAbstractClass)>
    Friend Class ImplementInterfaceCodeFixProvider
        Inherits CodeFixProvider
        Implements ReportCrashDumpsToMicrosoft

        Friend Const BC30149 As String = "BC30149" ' Class 'bar' must implement 'Sub foo()' for interface 'ifoo'.

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30149)
            End Get
        End Property

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return
            End If

            Dim implementsNode = token.GetAncestors(Of ImplementsStatementSyntax) _
                                 .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))
            If implementsNode Is Nothing Then
                Return
            End If

            Dim typeNode = implementsNode.Types.Where(Function(c) c.Span.IntersectsWith(span)) _
                           .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))

            If typeNode Is Nothing Then
                Return
            End If

            Dim service = document.GetLanguageService(Of IImplementInterfaceService)()
            Dim actions = service.GetCodeActions(
                document,
                Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False),
                typeNode,
                cancellationToken)

            context.RegisterFixes(actions, context.Diagnostics)
        End Function
    End Class
End Namespace
