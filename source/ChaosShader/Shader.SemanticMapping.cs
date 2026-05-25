using ChaosFramework.Collections;
using ChaosFramework.Core;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    partial class Shader
    {
        /// <summary>
        /// Stores the offsets (measured in bytes) of each variable with a semantic in a given ordered list of input semantics.
        /// <para> This is effectively just a cache. </para>
        /// </summary>
        public class SemanticMapping
        {
            static StringBuilder strBuilder = new StringBuilder();
            
            static SysCol.Dictionary<string, SemanticMapping> allMappings = new SysCol.Dictionary<string, SemanticMapping>();

            readonly string key;

            SemanticMapping(string key, LinkedList<ShaderParam> mappings)
            {
                this.key = key;
                int attributeAggregate = 0;
                foreach (ShaderParam param in mappings)
                {
                    mapping[param.semantic] = attributeAggregate;
                    attributeAggregate += GetNumAttribs(param.type);
                }
            }

            public static SemanticMapping GetMapping(LinkedList<ShaderParam> semanticList)
            {
                strBuilder.Clear();
                foreach (ShaderParam semantic in semanticList)
                    strBuilder.Append(semantic.semantic);

                string key = strBuilder.ToString();
                SemanticMapping output;
                if (!allMappings.TryGetValue(key, out output))
                    output = new SemanticMapping(key, semanticList);

                return output;
            }

            public SysCol.Dictionary<string, int> mapping = new SysCol.Dictionary<string, int>();

            int usageCount;

            public void AddUser(Disposable user)
            {
                usageCount++;
                user.AddOnDispose(Unuse);
            }

            void Unuse()
            {
                usageCount--;
                allMappings.Remove(key);
            }
        }
    }
}
