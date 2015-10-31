﻿using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Roslyn.SonarQube
{
    /// <summary>
    /// Generates SQ specific rule metadata from a Roslyn analyser assembly
    /// </summary>
    public class DiagnosticAssemblyScanner
    {
        ILogger logger;

        public DiagnosticAssemblyScanner(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Loads the given assembly and extracts information about existing types deriving from 
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        public IEnumerable<DiagnosticAnalyzer> ExtractDiagnosticsFromAssembly(string assemblyPath)
        {
            Assembly analyserAssembly = LoadAnalyzerAssembly(assemblyPath);
            IEnumerable<DiagnosticAnalyzer> analysers = Enumerable.Empty<DiagnosticAnalyzer>();

            if (analyserAssembly != null)
            {
                analysers = FetchDiagnosticAnalysers(analyserAssembly);
            }

            return analysers;
        }


        private Assembly LoadAnalyzerAssembly(string assemblyPath)
        {
            Assembly analyzerAssembly = null;
            try
            {
                analyzerAssembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(Resources.AssemblyLoadError, assemblyPath, ex.Message);
                return null;
            }

            return analyzerAssembly;
        }

        private static IEnumerable<DiagnosticAnalyzer> FetchDiagnosticAnalysers(Assembly analyserAssembly)
        {
            Debug.Assert(analyserAssembly != null);
            ICollection<DiagnosticAnalyzer> analysers = new List<DiagnosticAnalyzer>();

            foreach (Type type in analyserAssembly.GetExportedTypes())
            {
                if (!type.IsAbstract && type.IsSubclassOf(typeof(DiagnosticAnalyzer)))
                {
                    DiagnosticAnalyzer analyser = (DiagnosticAnalyzer)Activator.CreateInstance(type);
                    analysers.Add(analyser);
                }
            }

            return analysers;
        }
    }
}