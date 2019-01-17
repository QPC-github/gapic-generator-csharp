﻿// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// This test loads this source file during the test, parses it into
// Roslyn, removes all trivia, passes it through the Whitespace formatter,
// then checks that the input source and re-formatted source are identical.
// Code between the TEST_SOURCE_START and TEST_SOURCE_END comments are used
// as source for this test.
// So some of the source code looks a bit odd, as it needs to test every
// code construct is formatted correctly.
// TODO: Add further code as WhitespaceFormatter adds more formatting.

// TODO: Consider splitting this test into multiple files; possibly
// putting the test source separately, or splitting the test source
// into multiple source files, and/or multiple tests.

// TEST_SOURCE_START
using Google.Api.Generator.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using sys = System;

namespace Google.Api.Generator.Tests
{
    public class WhitespaceFormatterTest
    {
        // Test nested class; base-list with multiple items.
        // Test aliased type.
        private class Inner : List<sys::String>, IEnumerable<string>, IList<string>
        {
            // Test ctor with no parameters.
            // Test ctor calling another ctor with multiple parameters.
            // Test empty bock body.
            public Inner() : this(0, 1, 2)
            {
            }

            // Test ctor with single parameter.
            // Test ctor calling ctor with no args.
            // Test expression body.
            public Inner(int a) : this() => _a = a + 1;

            // Test ctor with multiple parameters.
            // Test non-empty block body.
            public Inner(int a, int b, int c)
            {
                // Test assignment.
                _a = a;
                // Test binary operator.
                _b = b + c;
            }

            // Test instance field with an initializer.
            private readonly int _a = -1;

            // Test instance field with no intializer.
            private readonly int _b;

            // Test get-only auto-property.
            public string P1 { get; }

            // Test get/set auto-property with initializer.
            public string P2 { get; set; } = "Hello_world!";

            // Test get-only expression-bodied property.
            public string P3
            {
                get => "Hello_another_world!";
            }

            private string _p4 = "";

            // Test get/set expression-bodied property.
            public string P4
            {
                get => _p4 + _p4;
                set => _p4 = value;
            }

            public class ObjectWithProperties
            {
                public string S { get; set; }

                public ObjectWithProperties O { get; set; }
            }

            // Test property with initializer using nested object initialization.
            public ObjectWithProperties P5 { get; set; } = new ObjectWithProperties
            {
                S = "A_string",
                O = new ObjectWithProperties
                {
                    S = "Another_string",
                },
            };

            public void M1()
            {
                // Test named argument
                string.Join(separator: ',', values: "a");
            }

            // Test expression-bodied method
            // Test `new` keyword
            public object GetObject() => new object();

            // Test `new` array with size.
            public string[] GetStringArrayWithSize() => new string[0];

            // Test `new` array without size, with initializer.
            public string[] GetStringArrayWithInitializer() => new string[]
            {
                "one",
            };
        }

        // Test base-list with one item.
        private class EmptyInner : List<string>
        {
            // Test class that has no members.
        }
        // TEST_SOURCE_END

        private class TriviaRemover : CSharpSyntaxRewriter
        {
            public TriviaRemover() : base(visitIntoStructuredTrivia: false) { }
            public override SyntaxNode Visit(SyntaxNode node)
            {
                return base.Visit(node)?.WithoutTrivia();
            }
            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                return base.VisitToken(token).WithoutTrivia();
            }
        }

        private static string ThisFilename([CallerFilePath] string filePath = null) => Path.GetFileName(filePath);

        [Fact]
        public void ThisSourceFile()
        {
            // Read source file and remove all ignored lines.
            var sourceLines = File.ReadAllLines(ThisFilename());
            var testSourceLines = sourceLines.Aggregate((ignoring: true, lines: new List<string>()), (acc, line) =>
            {
                var ignoring =
                    line.Trim() == "// TEST_SOURCE_END" ? true :
                    line.Trim() == "// TEST_SOURCE_START" ? false : (bool?)null;
                if (!acc.ignoring && ignoring != true && !line.Trim().StartsWith("//"))
                {
                    acc.lines.Add(line);
                }
                return (ignoring ?? acc.ignoring, acc.lines);
            });
            var testSource = string.Join("\r\n", testSourceLines.lines);
            // Parse source using Roslyn.
            var root = CSharpSyntaxTree.ParseText(testSource).GetCompilationUnitRoot();
            // Remove all trivia (including whitespace) from Roslyn tree.
            var rootWithoutWhitespace = new TriviaRemover().Visit(root);
            // Confirm that the source now contains no whitspace.
            var testSourceWithoutWhitespace = rootWithoutWhitespace.ToFullString();
            Assert.DoesNotContain(" ", testSourceWithoutWhitespace);
            Assert.DoesNotContain("\n", testSourceWithoutWhitespace);
            // Use the WhitespaceFormatter.
            var rootFormatter = new WhitespaceFormatter().Visit(rootWithoutWhitespace);
            var testSourceFormatter = rootFormatter.ToFullString();
            // Check that the sources are identical.
            Assert.Equal(testSource, testSourceFormatter);
        }
        // TEST_SOURCE_START
    }
}

// TEST_SOURCE_END