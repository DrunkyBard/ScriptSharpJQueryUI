﻿// Generator.cs
// ScriptSharpJQueryUI
//
// Copyright 2012 Ivaylo Gochkov
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ScriptSharpJQueryUI.Model;

namespace ScriptSharpJQueryUI {
    /// <summary>
    /// Script# jQueryUI API generator
    /// </summary>
    public partial class Generator {
        private string DestinationPath;
        private TextWriter Messages;

        /// <summary>
        /// Creates a generator of ScriptSharp jQueryUI library.
        /// </summary>
        /// <param name="destinationPath">Location of the generated files.</param>
        /// <param name="messageStream">A message stream.</param>
        public Generator(string destinationPath, TextWriter messageStream = null) {
            Debug.Assert(!string.IsNullOrEmpty(destinationPath), "Destination path is not specified.");

            DestinationPath = destinationPath;
            Messages = messageStream ?? TextWriter.Null;
        }

        /// <summary>
        /// Generates SriptSharp files
        /// </summary>
        /// <param name="entries">List of jQueryUI entries.</param>
        public void Render(IList<Entry> entries) {
            if (entries == null) {
                return;
            }

            DirectoryInfo destination = new DirectoryInfo(DestinationPath);
            if (destination.Exists) {
                destination.Delete(true);
            }

            foreach (Entry entry in entries) {
                Messages.WriteLine("Generating " + Path.Combine(DestinationPath, Utils.UppercaseFirst(entry.Name)));

                RenderEntry(entry);
            }

            Messages.WriteLine("Generating jQueryUI base files.");
            RenderJqueryUI();
            RenderJquerySize();
            RenderJqueryPosition();
        }

        private void RenderEntry(Entry entry) {
            if (entry == null) {
                return;
            }

            RenderObject(entry);
            RenderOptions(entry);
            RenderEvents(entry);

            RenderOptionEnum(entry);
            RenderEventsEnum(entry);
            RenderMethodEnum(entry);
        }

        private void RenderObject(Entry entry) {
            string className = Utils.UppercaseFirst(entry.Name) + @"Object";

            string content =
@"using System.Runtime.CompilerServices;

namespace jQueryApi.UI {{
    /// <summary>
    /// {1}
    /// </summary>
    /// <remarks>
    /// {2}
    /// </remarks>
    /// <example>
    /// {3}
    /// </example>
    [Imported]
    [IgnoreNamespace]
    public sealed class {0} : jQueryObject {{{4}{5}{6}
    }}
}}";

            string overload1 =
@"

        [ScriptName(""{0}"")]
        public extern {1}Object {1}();";

            string overload2 =
@"

        [ScriptName(""{0}"")]
        public extern {1}Object {1}({1}Options options);";

            string overload3 =
@"

        [ScriptName(""{0}"")]
        public extern object {1}({1}Method method, params object[] options);";

            string example = @"{0}
    /// <code>
    /// {1}
    /// </code>
    /// <code>
    /// {2}
    /// </code>";

            string formatedContent
                = string.Format(content
                                , className
                                , Utils.FormatXmlComment(entry.Description)
                                , Utils.FormatXmlComment(entry.LongDescription)
                                , (entry.Example != null) ? string.Format(example, Utils.FormatXmlComment(entry.Example.Description), Utils.FormatXmlComment(entry.Example.Code), Utils.FormatXmlComment(entry.Example.Html)) : string.Empty
                                , string.Format(overload1, entry.Name, Utils.UppercaseFirst(entry.Name))
                                , (entry.Options.Count > 0) ? string.Format(overload2, entry.Name, Utils.UppercaseFirst(entry.Name)) : string.Empty
                                , (entry.Methods.Count > 0) ? string.Format(overload3, entry.Name, Utils.UppercaseFirst(entry.Name)) : string.Empty);

            Utils.CreateFile(DestinationPath, Utils.UppercaseFirst(entry.Name), className, formatedContent);
        }

        private void RenderOptions(Entry entry) {
            string className = Utils.UppercaseFirst(entry.Name) + @"Options";

            string content =
@"using System;
using System.Runtime.CompilerServices;

namespace jQueryApi.UI {{
    [Imported]
    [IgnoreNamespace]
    [ScriptName(""Object"")]
    public sealed class {0} {{
        public {0}() {{ }}
        public {0}(params object[] nameValuePairs) {{ }}

{1}
{2}
    }}
}}";
            StringBuilder eventsContent = new StringBuilder();

            foreach (Event @event in entry.Events.AsQueryable().OrderBy(e => e.Name)) {
                if (!@event.Type.StartsWith("function")) {
                    if (!string.IsNullOrEmpty(@event.Description)) {
                        eventsContent.Append(@"        /// <summary>
        /// " + Utils.FormatXmlComment(@event.Description) + @"
        /// </summary>");
                    }

                    string eventType;

                    if (string.IsNullOrEmpty(@event.Type)) {
                        eventType = "jQueryEventHandler";
                    } else {
                        eventType = Utils.UppercaseFirst(@event.Type.Replace(@event.Name.ToLower(), Utils.UppercaseFirst(@event.Name))) + @"EventHandler";
                    }

                    eventsContent.AppendLine(@"
        [IntrinsicProperty]
        public " + eventType + " " + Utils.UppercaseFirst(@event.Name) + " { get { return null; } set { } }");
                }
            }

            StringBuilder optionsContent = new StringBuilder();

            foreach (var option in entry.Options.AsQueryable()
                                           .OrderBy(o => o.Name)
                                           .GroupBy(o => o.Name)) {
                if (!string.IsNullOrEmpty(option.Min(o => o.Description))) {
                    optionsContent.Append(@"        /// <summary>
        /// " + Utils.FormatXmlComment(option.Min(o => o.Description)) + @"
        /// </summary>");
                }

                optionsContent.AppendLine(@"
        [IntrinsicProperty]
        public " + Utils.GetCSType(option.Min(o => o.Type)) + @" " + Utils.UppercaseFirst(option.Key) + @" { get { return " + Utils.GetDefaultValue(option.Min(o => o.Type)) + @"; } set { } }");
            }



            Utils.CreateFile(DestinationPath, Utils.UppercaseFirst(entry.Name), className
                , string.Format(content, className, eventsContent.ToString(), optionsContent.ToString()));
        }

        private void RenderEventHandler(string entryName, string eventType) {
            string className = Utils.UppercaseFirst(eventType) + "EventHandler";

            string content =
@"using System.Runtime.CompilerServices;

namespace jQueryApi.UI {
    [Imported]
    [IgnoreNamespace]
    public delegate void " + className + @"(jQueryEvent e, " + Utils.UppercaseFirst(eventType) + "Event " + eventType + @"Event);
}";

            Utils.CreateFile(DestinationPath, entryName, className, content);
        }

        private void RenderEvents(Entry entry) {
            if (entry.Events.Count == 0) {
                return;
            }

            string content =
@"using System.Runtime.CompilerServices;

namespace jQueryApi.UI {{
    [Imported]
    [IgnoreNamespace]
    [ScriptName(""Object"")]
    public sealed class {0} {{
{1}
    }}
}}";
            string property = @"
        [IntrinsicProperty]
        public {1} {0} {{ get {{ return {2}; }} set {{ }} }}";

            string className;

            foreach (var @event in entry.Events.AsQueryable()
                                          .OrderBy(e => e.Name)) {
                if (@event.Type.StartsWith("function")) continue;
                if (string.IsNullOrEmpty(@event.Type)) continue;

                string eventType = @event.Type.Replace(@event.Name.ToLower(), Utils.UppercaseFirst(@event.Name));

                foreach (Argument arg in @event.Arguments) {
                    if (arg.Name != "ui") continue;

                    className = Utils.UppercaseFirst(eventType) + "Event";

                    StringBuilder properties = new StringBuilder();

                    foreach (Property prop in arg.Properties.OrderBy(p => p.Name)) {
                        properties.Append(string.Format(property, Utils.UppercaseFirst(prop.Name), Utils.GetCSType(prop.Type), Utils.GetDefaultValue(prop.Type)));
                    }

                    RenderEventHandler(Utils.UppercaseFirst(entry.Name), eventType);

                    Utils.CreateFile(DestinationPath, Utils.UppercaseFirst(entry.Name)
                                    , className
                                    , string.Format(content, className, properties.ToString()));
                }
            }
        }

        private void RenderOptionEnum(Entry entry) {
            if (entry.Options.Count == 0) {
                return;
            }

            string className = Utils.UppercaseFirst(entry.Name) + @"Option";

            string content =
@"using System.Runtime.CompilerServices;

namespace jQueryApi.UI {{
    [Imported]
    [IgnoreNamespace]
    [NamedValues]
    public enum {0} {{{1}
    }}

}}";
            StringBuilder enumValues = new StringBuilder();

            foreach (var option in entry.Options.AsQueryable()
                                           .OrderBy(o => o.Name)
                                           .GroupBy(o => o.Name)) {
                enumValues.AppendLine();
                enumValues.AppendLine("        /// <summary>");
                enumValues.AppendLine("        /// " + Utils.FormatXmlComment(option.Min(o => o.Description)));
                enumValues.AppendLine("        /// </summary>");
                enumValues.Append("        " + Utils.UppercaseFirst(option.Key) + ",");
            }

            Utils.CreateFile(DestinationPath, Utils.UppercaseFirst(entry.Name), className
                , string.Format(content, className, enumValues.ToString().Trim(',')));
        }

        private void RenderEventsEnum(Entry entry) {
            if (entry.Events.Count == 0) {
                return;
            }

            string className = Utils.UppercaseFirst(entry.Name) + @"Event";

            string content =
@"using System.Runtime.CompilerServices;

namespace jQueryApi.UI {{
    [Imported]
    [IgnoreNamespace]
    [NamedValues]
    public enum {0} {{{1}
    }}

}}";
            StringBuilder enumValues = new StringBuilder();

            foreach (var @event in entry.Events.AsQueryable()
                                          .OrderBy(e => e.Name)) {
                enumValues.AppendLine();
                enumValues.AppendLine("        /// <summary>");
                enumValues.AppendLine("        /// " + Utils.FormatXmlComment(@event.Description));
                enumValues.AppendLine("        /// </summary>");
                enumValues.Append("        " + Utils.UppercaseFirst(@event.Name) + ",");
            }

            Utils.CreateFile(DestinationPath, Utils.UppercaseFirst(entry.Name), className
                , string.Format(content, className, enumValues.ToString().Trim(',')));
        }

        private void RenderMethodEnum(Entry entry) {
            if (entry.Methods.Count == 0) {
                return;
            }

            string className = Utils.UppercaseFirst(entry.Name) + @"Method";

            string content =
@"using System.Runtime.CompilerServices;

namespace jQueryApi.UI {{
    [Imported]
    [IgnoreNamespace]
    [NamedValues]
    public enum {0} {{{1}
    }}

}}";
            StringBuilder enumValues = new StringBuilder();

            foreach (var method in entry.Methods.AsQueryable()
                                           .OrderBy(m => m.Name)
                                           .GroupBy(m => m.Name)) {
                enumValues.AppendLine();
                enumValues.AppendLine("        /// <summary>");
                enumValues.AppendLine("        /// " + Utils.FormatXmlComment(method.Min(m => m.Description)));
                enumValues.AppendLine("        /// </summary>");
                enumValues.Append("        " + Utils.UppercaseFirst(method.Key) + ",");
            }

            Utils.CreateFile(DestinationPath, Utils.UppercaseFirst(entry.Name), className
                , string.Format(content, className, enumValues.ToString().Trim(',')));
        }

        private void RenderJqueryUI() {
            string className = "jQueryObjectUI";

            string content = @"using System.Runtime.CompilerServices;

namespace jQueryApi.UI
{
    [Imported]
    [IgnoreNamespace]
    public class " + className + @" : jQueryObject
    {
        public jQueryObjectUI scrollParent()
        {
            return null;
        }

        public jQueryObjectUI zIndex()
        {
            return null;
        }

        /// <summary>
        /// Disables text selection in the matched elements.
        /// Not documented yet jQuery extension!
        /// </summary>
        /// <returns>The current jQueryObject.</returns>
        public jQueryObjectUI DisableSelection()
        {
            return null;
        }

        /// <summary>
        /// Enables text selection in the matched elements.
        /// Not documented yet jQuery extension!
        /// </summary>
        /// <returns>The current jQueryObject.</returns>
        public jQueryObjectUI EnableSelection()
        {
            return null;
        }
    }
}";
            Utils.CreateFile(DestinationPath, string.Empty, className, content);
        }

        private void RenderJquerySize() {
            string className = "jQuerySize";

            string content = @"using System.Runtime.CompilerServices;

namespace jQueryApi.UI
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName(""Object"")]
    public sealed class " + className + @" {
        [IntrinsicProperty]
        public string Width { get { return null; } set { } }

        [IntrinsicProperty]
        public string Height { get { return null; } set { } }
    }
}";
            Utils.CreateFile(DestinationPath, string.Empty, className, content);
        }

        private void RenderJqueryPosition() {
            string className = "jQueryPosition";
            string content = @"using System.Runtime.CompilerServices;

namespace jQueryApi.UI
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName(""Object"")]
    public sealed class " + className + @" {
        [IntrinsicProperty]
        public string Top { get { return null; } set { } }

        [IntrinsicProperty]
        public string Left { get { return null; } set { } }
    }
}";
            Utils.CreateFile(DestinationPath, string.Empty, className, content);
        }

        /// <summary>
        /// Renders project file with included all generated files.
        /// </summary>
        /// <param name="entries">List of jQueryUI entries.</param>
        public void RenderProjectFile(IList<Entry> entries) {
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{824C1FEC-2455-4183-AFC6-891EDB88213A}</ProjectGuid>
    <OutputType>Library</OutputType>
    <NoStdLib>True</NoStdLib>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>jQueryApi.UI</RootNamespace>
    <AssemblyName>Script.jQuery.UI</AssemblyName>
    <TargetFrameworkVersion>v2.0</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <TargetFrameworkProfile />
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <DocumentationFile>bin\Debug\Script.jQuery.UI.xml</DocumentationFile>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <DocumentationFile>bin\Release\Script.jQuery.UI.xml</DocumentationFile>
  </PropertyGroup>
  <ItemGroup>
";
            foreach (Entry entry in entries) {
                content += @"<Compile Include=""" + Utils.UppercaseFirst(entry.Name) + @"\*.cs"" />
";
            }

            content += @"    <Compile Include=""jQueryObjectUI.cs"" />
    <Compile Include=""jQuerySize.cs"" />
    <Compile Include=""jQueryPosition.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
    <None Include=""Properties\ScriptInfo.txt"">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  <ItemGroup>
    <Reference Include=""mscorlib, Version=0.7.0.0, Culture=neutral, PublicKeyToken=8fc0e3af5abcb6c4, processorArchitecture=MSIL"" />
    <Reference Include=""Script.jQuery, Version=0.7.0.0, Culture=neutral, PublicKeyToken=8fc0e3af5abcb6c4, processorArchitecture=MSIL"" />
    <Reference Include=""Script.Web, Version=0.7.0.0, Culture=neutral, PublicKeyToken=8fc0e3af5abcb6c4, processorArchitecture=MSIL"" />
    <Reference Include=""System"" />
  </ItemGroup>
  <Import Project=""$(MSBuildBinPath)\Microsoft.CSharp.targets"" />
  <Target Name=""AfterBuild"">
    <Copy SourceFiles=""@(ScriptInfo)"" DestinationFiles=""$(OutputPath)$(AssemblyName).txt"" />
  </Target>
</Project>";

            using (StreamWriter file = new StreamWriter(Path.Combine(DestinationPath, "jQueryApi.UI.csproj"))) {
                file.WriteLine(content);
            }

            // render assembly information
            string assemblyContent = @"﻿
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle(""Script.jQuery.UI"")]
[assembly: AssemblyDescription(""Script# jQuery UI Plugin"")]
[assembly: ScriptAssembly(""jQueryUI"")]";

            Utils.CreateFile(DestinationPath, "Properties", "AssemblyInfo", assemblyContent);

            // render script information
            string infoContent = @"jQuery UI
===============================================================================

This assembly provides access to jQuery UI APIs. This is only meant for use at
development time, so you can reference and compile your c# code against 
the plugin APIs. You must include the appropriate plugin scripts in your page for 
runtime functionality.

More information is on http://jqueryui.com.

-------------------------------------------------------------------------------

Associated scripts can be found at
http://jqueryui.com/download";

            using (StreamWriter file = new StreamWriter(Path.Combine(DestinationPath, "Properties", "ScriptInfo.txt"))) {
                file.WriteLine(infoContent);
            }
        }
    }
}
