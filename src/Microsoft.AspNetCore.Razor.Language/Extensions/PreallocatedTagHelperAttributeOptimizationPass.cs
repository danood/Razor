﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal class PreallocatedTagHelperAttributeOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        public override int Order => DefaultFeatureOrder;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var walker = new PreallocatedTagHelperWalker();
            walker.VisitDocument(documentNode);
        }

        internal class PreallocatedTagHelperWalker : IntermediateNodeWalker
        {
            private const string PreAllocatedAttributeVariablePrefix = "__tagHelperAttribute_";

            private ClassDeclarationIntermediateNode _classDeclaration;
            private int _variableCountOffset;
            private int _preallocatedDeclarationCount = 0;

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                _classDeclaration = node;
                _variableCountOffset = node.Children.Count;

                VisitDefault(node);
            }

            public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIntermediateNode node)
            {
                if (node.Children.Count != 1 || !(node.Children.First() is HtmlContentIntermediateNode))
                {
                    return;
                }

                var htmlContentNode = node.Children.First() as HtmlContentIntermediateNode;
                var plainTextValue = GetContent(htmlContentNode);

                DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode declaration = null;

                for (var i = 0; i < _classDeclaration.Children.Count; i++)
                {
                    var current = _classDeclaration.Children[i];

                    if (current is DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode existingDeclaration)
                    {
                        if (string.Equals(existingDeclaration.Name, node.Name, StringComparison.Ordinal) &&
                            string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                            existingDeclaration.AttributeStructure == node.AttributeStructure)
                        {
                            declaration = existingDeclaration;
                            break;
                        }
                    }
                }

                if (declaration == null)
                {
                    var variableCount = _classDeclaration.Children.Count - _variableCountOffset;
                    var preAllocatedAttributeVariableName = PreAllocatedAttributeVariablePrefix + variableCount;
                    declaration = new DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode
                    {
                        VariableName = preAllocatedAttributeVariableName,
                        Name = node.Name,
                        Value = plainTextValue,
                        AttributeStructure = node.AttributeStructure,
                    };
                    _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
                }

                var addPreAllocatedAttribute = new AddPreallocatedTagHelperHtmlAttributeIntermediateNode
                {
                    VariableName = declaration.VariableName,
                };

                var nodeIndex = Parent.Children.IndexOf(node);
                Parent.Children[nodeIndex] = addPreAllocatedAttribute;
            }

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIntermediateNode node)
            {
                if (!(node.Descriptor.IsStringProperty || (node.IsIndexerNameMatch && node.Descriptor.IsIndexerStringProperty)) ||
                    node.Children.Count != 1 ||
                    !(node.Children.First() is HtmlContentIntermediateNode))
                {
                    return;
                }

                var htmlContentNode = node.Children.First() as HtmlContentIntermediateNode;
                var plainTextValue = GetContent(htmlContentNode);

                DeclarePreallocatedTagHelperAttributeIntermediateNode declaration = null;

                for (var i = 0; i < _classDeclaration.Children.Count; i++)
                {
                    var current = _classDeclaration.Children[i];

                    if (current is DeclarePreallocatedTagHelperAttributeIntermediateNode existingDeclaration)
                    {
                        if (string.Equals(existingDeclaration.Name, node.AttributeName, StringComparison.Ordinal) &&
                            string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                            existingDeclaration.AttributeStructure == node.AttributeStructure)
                        {
                            declaration = existingDeclaration;
                            break;
                        }
                    }
                }

                if (declaration == null)
                {
                    var variableCount = _classDeclaration.Children.Count - _variableCountOffset;
                    var preAllocatedAttributeVariableName = PreAllocatedAttributeVariablePrefix + variableCount;
                    declaration = new DeclarePreallocatedTagHelperAttributeIntermediateNode
                    {
                        VariableName = preAllocatedAttributeVariableName,
                        Name = node.AttributeName,
                        Value = plainTextValue,
                        AttributeStructure = node.AttributeStructure,
                    };
                    _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
                }

                var setPreallocatedProperty = new SetPreallocatedTagHelperPropertyIntermediateNode
                {
                    VariableName = declaration.VariableName,
                    AttributeName = node.AttributeName,
                    TagHelperTypeName = node.TagHelperTypeName,
                    PropertyName = node.PropertyName,
                    Descriptor = node.Descriptor,
                    Binding = node.Binding,
                    IsIndexerNameMatch = node.IsIndexerNameMatch,
                };

                var nodeIndex = Parent.Children.IndexOf(node);
                Parent.Children[nodeIndex] = setPreallocatedProperty;
            }

            private string GetContent(HtmlContentIntermediateNode node)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsHtml)
                    {
                        builder.Append(token.Content);
                    }
                }

               return builder.ToString();
            }
        }
    }
}