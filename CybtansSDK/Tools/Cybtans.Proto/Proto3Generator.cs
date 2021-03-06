﻿#nullable enable

using Antlr4.Runtime;
using System.Text;
using System.IO;
using Cybtans.Proto.AST;
using System.Linq;
using System;

namespace Cybtans.Proto
{
    public class Proto3Generator
    {
        readonly ProtoParserErrorListener _errorListener = new ProtoParserErrorListener();
        readonly ErrorReporter _reporter = new ErrorReporter();
        readonly Scope _scope;
        readonly IFileResolverFactory _fileResolverFactory;

        public Proto3Generator(IFileResolverFactory fileResolverFactory)
        {
            _fileResolverFactory = fileResolverFactory;
            _scope = Scope.CreateRootScope();
        }
               

        public (ProtoFile File, Scope Scope ) LoadFromFile(string filename)
        {
            var content = File.ReadAllText(filename);
            ICharStream stream = CharStreams.fromstring(content);
            ITokenSource lexer = new Protobuf3Lexer(stream);
            ITokenStream tokens = new CommonTokenStream(lexer);
            Protobuf3Parser parser = new Protobuf3Parser(tokens);
            
            parser.AddErrorListener(_errorListener);
            var context = parser.proto();            
            _errorListener.EnsureNoErrors();

            var node = context.file;
            var scope = _scope.CreateScope();

            if (node.Imports.Any())
            {
                LoadImports(node, scope, filename);
            }

            node.CheckSemantic(scope, _reporter);

            _reporter.EnsureNoErrors();
            return (node, scope);
        }

        private void LoadImports(ProtoFile file, Scope scope, string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            if (directory == null)
                throw new InvalidOperationException($"Base directory not found for {filename}");           

            var fileResolver = _fileResolverFactory.GetResolver(directory);

            foreach (var import in file.Imports)
            {                
                var importFile = fileResolver.GetFile(import.Name);
                
                var (importFileNode, importScope) = LoadFromFile(importFile.FullName);

                file.ImportedFiles.Add(importFileNode);

                if(importFileNode.Package != null && !importFileNode.Package.Equals(file.Package))
                {
                    scope.AddPackage(importFileNode.Package.Id, importScope);
                }
                else
                {
                    if (file.Package != null)
                    {
                        importFileNode.Package = file.Package;
                        if (importFileNode.Option.Namespace == null)
                            importFileNode.Option.Namespace = file.Option.Namespace ?? file.Package.ToString();
                    }

                    scope.Merge(importScope);
                    file.Merge(importFileNode);
                }
            }
        }
              
    }

   

}
