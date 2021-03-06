﻿using Cybtans.Proto.AST;
using Cybtans.Proto.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cybtans.Proto.Generators.CSharp
{
    public class ServiceGenerator : FileGenerator<TypeGeneratorOption>
    {
        private TypeGenerator _typeGenerator;

        public ServiceGenerator(ProtoFile proto, TypeGeneratorOption option, TypeGenerator typeGenerator) :base(proto, option)
        {
            this._typeGenerator = typeGenerator;
            Namespace = option.Namespace ?? $"{proto.Option.Namespace}.Services";

            foreach (var item in Proto.Declarations)
            {
                if (item is ServiceDeclaration srv)
                {
                    var info = new ServiceGenInfo(srv, _option, Proto);             

                    Services.Add(srv, info);
                }
            }
        }

        public string Namespace { get; }

        public Dictionary<ServiceDeclaration, ServiceGenInfo> Services { get; } = new Dictionary<ServiceDeclaration, ServiceGenInfo>();

        public override void GenerateCode()
        {
            Directory.CreateDirectory(_option.OutputPath);

            foreach (var item in Services)
            {               
                GenerateService(item.Value);                    
            }
        }        

        private void GenerateService(ServiceGenInfo info)
        {
            var writer = CreateWriter(info.Namespace);
           
            writer.Usings.Append("using System.Threading.Tasks;").AppendLine();
            writer.Usings.Append($"using {_typeGenerator.Namespace};").AppendLine();
            writer.Usings.Append("using System.Collections.Generic;").AppendLine();

            var clsWriter = writer.Class;

            if (info.Service.Option.Description != null)
            {
                clsWriter.Append("/// <summary>").AppendLine();
                clsWriter.Append("/// ").Append(info.Service.Option.Description).AppendLine();
                clsWriter.Append("/// </summary>").AppendLine();                
            }

            clsWriter.Append("public");

            if (_option.PartialClass)
            {
                clsWriter.Append(" partial");
            }

            clsWriter.Append($" interface I{info.Name} ").AppendLine();                 
                        
            clsWriter.Append("{").AppendLine();
            clsWriter.Append('\t', 1);

            var bodyWriter = clsWriter.Block("BODY");

            foreach (var rpc in info.Service.Rpcs)
            {
                var returnInfo = rpc.ResponseType;
                var requestInfo = rpc.RequestType;               

                bodyWriter.AppendLine();
                if (rpc.Option.Description != null)
                {
                    bodyWriter.Append("/// <summary>").AppendLine();
                    bodyWriter.Append("/// ").Append(rpc.Option.Description).AppendLine();
                    bodyWriter.Append("/// </summary>").AppendLine();
                }

                bodyWriter.Append($"{returnInfo.GetReturnTypeName()} { GetRpcName(rpc)}({requestInfo.GetRequestTypeName("request")});");
                bodyWriter.AppendLine();
                bodyWriter.AppendLine();
            }

            clsWriter.Append("}").AppendLine();

            writer.Save($"{info.Name}Contract");
        }

        public string GetRpcName(RpcDeclaration rpc)
        {
            return rpc.Name.Pascal();
        }  
        
      
    }
}
