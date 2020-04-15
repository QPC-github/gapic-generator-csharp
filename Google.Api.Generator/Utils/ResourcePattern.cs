﻿// Copyright 2020 Google Inc. All Rights Reserved.
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Google.Api.Generator.Utils
{
    internal class ResourcePattern
    {
        public class Segment
        {
            public Segment(string segment)
            {
                var sb = new StringBuilder();
                var inSep = true;
                var separators = new List<string>();
                var parameterNames = new List<string>();
                foreach (var c in segment)
                {
                    if (c == '{')
                    {
                        if (!inSep)
                        {
                            throw new ArgumentException($"Segment '{segment}' is ill-formed; incorrect '{{' found.");
                        }
                        if (separators.Count > 0 && sb.Length == 0)
                        {
                            throw new ArgumentException($"Segment '{segment}' is ill-formed; separator cannot be empty.");
                        }
                        separators.Add(sb.ToString());
                        sb.Clear();
                        inSep = false;
                    }
                    else if (c == '}')
                    {
                        if (inSep)
                        {
                            throw new ArgumentException($"Segment '{segment}' is ill-formed; incorrect '}}' found.");
                        }
                        parameterNames.Add(sb.ToString());
                        sb.Clear();
                        inSep = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                if (!inSep)
                {
                    throw new ArgumentException($"Segment '{segment}' is ill-formed; missing final '}}'.");
                }
                separators.Add(sb.ToString());
                Separators = separators;
                ParameterNames = parameterNames;
            }

            // Seperators include pre- and post- non-parameter-names; there will always be one more seperator than parameter-name.
            public IReadOnlyList<string> Separators { get; }
            public IReadOnlyList<string> ParameterNames { get; }
            public int ParameterCount => ParameterNames.Count;
            public bool IsComplex => ParameterCount > 1 || (ParameterCount == 1 && Separators.Any(x => x != ""));

            public string Expand(IEnumerable<string> parameters) => Separators[0] + string.Join("", parameters.Zip(Separators.Skip(1), (p, s) => p + s));

            public string PathTemplateString => ParameterCount > 0 ? "{" + string.Join('_', ParameterNames) + "}" : Separators.Single();
        }

        public ResourcePattern(string pattern) => Segments = pattern.Split('/').Select(x => new Segment(x)).ToList();

        public IReadOnlyList<Segment> Segments { get; }

        public IEnumerable<string> ParameterNames => Segments.SelectMany(x => x.ParameterNames);

        public string Expand(IEnumerable<string> parameters) => string.Join('/',
            Segments.Aggregate((result: ImmutableList<string>.Empty, paramOfs: 0),
                (acc, seg) => (acc.result.Add(seg.Expand(parameters.Skip(acc.paramOfs))), acc.paramOfs + seg.ParameterCount),
                acc => acc.result));

        public string PathTemplateString => string.Join('/', Segments.Select(x => x.PathTemplateString));
    }
}