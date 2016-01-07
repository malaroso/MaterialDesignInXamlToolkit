﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;

namespace mdresgen
{
    class IconThing
    {
        public void Run()
        {            
            Console.WriteLine("Downloading icon data...");

            //var nameDataPairs = GetNameDataPairs(GetSourceData()).ToList();

//            Console.WriteLine(nameDataPairs.Count);

            UpdateEnum("IconType.template.cs");
        }

        private static string GetSourceData()
        {
            var webRequest = WebRequest.CreateDefault(
                new Uri("https://materialdesignicons.com/api/package/38EF63D0-4744-11E4-B3CF-842B2B6CFE1B"));
            webRequest.UseDefaultCredentials = true;
            using (var sr = new StreamReader(webRequest.GetResponse().GetResponseStream()))
            {
                var iconData = sr.ReadToEnd();

                Console.WriteLine("Got.");

                return iconData;
            }
        }

        private static IEnumerable<Tuple<string, string>> GetNameDataPairs(string sourceData)
        {
            var jObject = JObject.Parse(sourceData);
            return jObject["icons"].Select(t => new Tuple<string, string>(
                t["name"].ToString().Underscore().Pascalize(), 
                t["data"].ToString()));
        }


        private void UpdateEnum(string sourceFile)
        {
            var sourceText = SourceText.From(new FileStream(sourceFile, FileMode.Open));
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);

            var rootNode = syntaxTree.GetRoot();
            var namespaceDeclarationNode = rootNode.ChildNodes().Single();
            var enumDeclarationSyntaxNode = namespaceDeclarationNode.ChildNodes().OfType<EnumDeclarationSyntax>().Single();            

            var emptyEnumDeclarationSyntaxNode = enumDeclarationSyntaxNode.RemoveNodes(enumDeclarationSyntaxNode.ChildNodes().OfType<EnumMemberDeclarationSyntax>(), SyntaxRemoveOptions.KeepDirectives);

            var leadingTriviaList = SyntaxTriviaList.Create(SyntaxFactory.Whitespace("        "));
            var generatedEnumDeclarationSyntax = emptyEnumDeclarationSyntaxNode.AddMembers(
                SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.Identifier(leadingTriviaList, "Aston", SyntaxTriviaList.Empty)),
                SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.Identifier(leadingTriviaList, "Villa", SyntaxTriviaList.Empty)));

            generatedEnumDeclarationSyntax = AddLineFeedsToCommas(generatedEnumDeclarationSyntax);
                
            var generatedNamespaceDeclarationSyntaxNode = namespaceDeclarationNode.ReplaceNode(enumDeclarationSyntaxNode, generatedEnumDeclarationSyntax);
            var generatedRootNode = rootNode.ReplaceNode(namespaceDeclarationNode, generatedNamespaceDeclarationSyntaxNode);
            
            Console.WriteLine(generatedRootNode.ToFullString());
        }

        private static EnumDeclarationSyntax AddLineFeedsToCommas(EnumDeclarationSyntax enumDeclarationSyntax)
        {
            var none = new SyntaxToken();
            var trailingTriviaList = SyntaxTriviaList.Create(SyntaxFactory.ElasticCarriageReturnLineFeed);

            Func<EnumDeclarationSyntax, SyntaxToken> next = enumSyntax => enumSyntax.ChildNodesAndTokens()
                .Where(nodeOrToken => nodeOrToken.IsToken)
                .Select(nodeOrToken => nodeOrToken.AsToken())
                .FirstOrDefault(
                    token =>
                        token.Value.Equals(",") &&
                        (!token.HasTrailingTrivia || !token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia)));

            SyntaxToken current;
            while ((current = next(enumDeclarationSyntax)) != none)
            {
                enumDeclarationSyntax = enumDeclarationSyntax.ReplaceToken(current,
                    SyntaxFactory.Identifier(SyntaxTriviaList.Empty, ",", trailingTriviaList)
                    );
            }

            return enumDeclarationSyntax;
        }
    }
}
