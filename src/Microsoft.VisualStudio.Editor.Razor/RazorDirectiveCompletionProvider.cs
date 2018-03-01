﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(IAsyncCompletionServiceProvider))]
    [Name("RazorCoreCompletionService")]
    [ContentType(RazorLanguage.ContentType)]

    internal class FooCompletionProvider : IAsyncCompletionServiceProvider
    {
        private readonly CompletionService _completionService;

        public FooCompletionProvider()
        {
            _completionService = new CompletionService();
        }

        public IAsyncCompletionService GetOrCreate(ITextView textView)
        {
            return _completionService;
        }
    }

    internal class CompletionService : IAsyncCompletionService
    {
        public Task<ImmutableArray<Language.Intellisense.CompletionItem>> SortCompletionListAsync(
            ImmutableArray<Language.Intellisense.CompletionItem> initialList, 
            CompletionTriggerReason triggerReason, 
            ITextSnapshot snapshot, 
            ITrackingSpan applicableToSpan, 
            ITextView view, 
            CancellationToken token)
        {
            return Task.FromResult(initialList);
        }

        public Task<FilteredCompletionModel> UpdateCompletionListAsync(
            ImmutableArray<Language.Intellisense.CompletionItem> sortedList, 
            CompletionTriggerReason triggerReason, 
            CompletionFilterReason filterReason, 
            ITextSnapshot snapshot, 
            ITrackingSpan applicableToSpan, 
            ImmutableArray<CompletionFilterWithState> selectedFilters, 
            ITextView view, 
            CancellationToken token)
        {
            var newList = sortedList.Select(l => new CompletionItemWithHighlight(l));

            var model = new FilteredCompletionModel(newList.ToImmutableArray(), 0);
            return Task.FromResult(model);
        }
    }

    //[System.Composition.Shared]
    //[Export(typeof(CompletionProvider))]
    //[ExportMetadata("Language", LanguageNames.CSharp)]
    //internal class RazorDirectiveCompletionProvider : CompletionProvider
    //{
    //    // Internal for testing
    //    internal static readonly string DescriptionKey = "Razor.Description";

    //    private static readonly IEnumerable<DirectiveDescriptor> DefaultDirectives = new[]
    //    {
    //        CSharpCodeParser.AddTagHelperDirectiveDescriptor,
    //        CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
    //        CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
    //    };
    //    private readonly Lazy<RazorCodeDocumentProvider> _codeDocumentProvider;

    //    [ImportingConstructor]
    //    public RazorDirectiveCompletionProvider([Import(typeof(RazorCodeDocumentProvider))] Lazy<RazorCodeDocumentProvider> codeDocumentProvider)
    //    {
    //        if (codeDocumentProvider == null)
    //        {
    //            throw new ArgumentNullException(nameof(codeDocumentProvider));
    //        }

    //        _codeDocumentProvider = codeDocumentProvider;
    //    }

    //    public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
    //    {
    //        if (document == null)
    //        {
    //            throw new ArgumentNullException(nameof(document));
    //        }

    //        if (item == null)
    //        {
    //            throw new ArgumentNullException(nameof(item));
    //        }

    //        var descriptionContent = new List<TaggedText>();
    //        if (item.Properties.TryGetValue(DescriptionKey, out var directiveDescription))
    //        {
    //            var descriptionText = new TaggedText(TextTags.Text, directiveDescription);
    //            descriptionContent.Add(descriptionText);
    //        }

    //        var completionDescription = CompletionDescription.Create(descriptionContent.ToImmutableArray());
    //        return Task.FromResult(completionDescription);
    //    }

    //    public override Task ProvideCompletionsAsync(CompletionContext context)
    //    {
    //        if (context == null)
    //        {
    //            throw new ArgumentNullException(nameof(context));
    //        }

    //        // FilePath will be null when the editor is open for cases where we don't have a file on disk (C# interactive window and others).
    //        if (context.Document?.FilePath == null ||
    //            !context.Document.FilePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
    //        {
    //            // Not a Razor file.
    //            return Task.CompletedTask;
    //        }

    //        var result = AddCompletionItems(context);

    //        return result;
    //    }

    //    // We do not want this inlined because the work done in this method requires Razor.Workspaces and Razor.Language assemblies.
    //    // If those two assemblies were to load you'd have them load in every C# editor completion scenario.
    //    [MethodImpl(MethodImplOptions.NoInlining)]
    //    private Task AddCompletionItems(CompletionContext context)
    //    {
    //        if (!_codeDocumentProvider.Value.TryGetFromDocument(context.Document, out var codeDocument))
    //        {
    //            // A Razor code document has not yet been associated with the document.
    //            return Task.CompletedTask;
    //        }

    //        var syntaxTree = codeDocument.GetSyntaxTree();
    //        if (syntaxTree == null)
    //        {
    //            // No syntax tree has been computed for the current document.
    //            return Task.CompletedTask;
    //        }

    //        if (!AtDirectiveCompletionPoint(syntaxTree, context))
    //        {
    //            // Can't have a valid directive at the current location.
    //            return Task.CompletedTask;
    //        }

    //        var completionItems = GetCompletionItems(syntaxTree);
    //        context.AddItems(completionItems);

    //        return Task.CompletedTask;
    //    }

    //    // Internal virtual for testing
    //    internal virtual IEnumerable<CompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree)
    //    {
    //        var directives = syntaxTree.Options.Directives.Concat(DefaultDirectives);
    //        var completionItems = new List<CompletionItem>();
    //        foreach (var directive in directives)
    //        {
    //            var propertyDictionary = new Dictionary<string, string>(StringComparer.Ordinal);

    //            if (!string.IsNullOrEmpty(directive.Description))
    //            {
    //                propertyDictionary[DescriptionKey] = directive.Description;
    //            }

    //            var completionItem = CompletionItem.Create(
    //                directive.Directive,
    //                // This groups all Razor directives together
    //                sortText: "_RazorDirective_",
    //                rules: CompletionItemRules.Create(formatOnCommit: false),
    //                tags: ImmutableArray.Create(CompletionTags.Intrinsic),
    //                properties: propertyDictionary.ToImmutableDictionary());
    //            completionItems.Add(completionItem);
    //        }

    //        return completionItems;
    //    }

    //    // Internal for testing
    //    internal bool AtDirectiveCompletionPoint(RazorSyntaxTree syntaxTree, CompletionContext context)
    //    {
    //        if (TryGetRazorSnapshotPoint(context, out var razorSnapshotPoint))
    //        {
    //            var change = new SourceChange(razorSnapshotPoint.Position, 0, string.Empty);
    //            var owner = syntaxTree.Root.LocateOwner(change);

    //            if (owner == null)
    //            {
    //                return false;
    //            }

    //            if (owner.ChunkGenerator is ExpressionChunkGenerator &&
    //                owner.Symbols.All(IsDirectiveCompletableSymbol) &&
    //                // Do not provide IntelliSense for explicit expressions. Explicit expressions will usually look like:
    //                // [@] [(] [DateTime.Now] [)]
    //                owner.Parent?.Children.Count > 1 &&
    //                owner.Parent.Children[1] == owner)
    //            {
    //                return true;
    //            }
    //        }

    //        return false;
    //    }

    //    protected virtual bool TryGetRazorSnapshotPoint(CompletionContext context, out SnapshotPoint snapshotPoint)
    //    {
    //        snapshotPoint = default(SnapshotPoint);

    //        if (context.Document.TryGetText(out var sourceText))
    //        {
    //            var textSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();
    //            var projectionSnapshot = textSnapshot as IProjectionSnapshot;

    //            if (projectionSnapshot == null)
    //            {
    //                return false;
    //            }

    //            var mappedPoints = projectionSnapshot.MapToSourceSnapshots(context.CompletionListSpan.Start);
    //            var htmlSnapshotPoints = mappedPoints.Where(p => p.Snapshot.TextBuffer.ContentType.IsOfType(RazorLanguage.ContentType));

    //            if (!htmlSnapshotPoints.Any())
    //            {
    //                return false;
    //            }

    //            snapshotPoint = htmlSnapshotPoints.First();
    //            return true;
    //        }

    //        return false;
    //    }

    //    private static bool IsDirectiveCompletableSymbol(AspNetCore.Razor.Language.Legacy.ISymbol symbol)
    //    {
    //        if (!(symbol is CSharpSymbol csharpSymbol))
    //        {
    //            return false;
    //        }

    //        return csharpSymbol.Type == CSharpSymbolType.Identifier ||
    //            // Marker symbol
    //            csharpSymbol.Type == CSharpSymbolType.Unknown;
    //    }
    //}
}
