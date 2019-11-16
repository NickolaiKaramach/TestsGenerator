using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestGeneratorLib.Entity;
using TestGeneratorLib.Util;

namespace TestGeneratorLib.OutputUtil
{
    public class OutputFileFormatter
    {
        private const int StringBuilderInitCapacity = 16 * 1024;
        private const string Tab = "    ";
        private string _testObjectVarName;
        private readonly StringBuilder _sb = new StringBuilder(StringBuilderInitCapacity);

        private int _stackFrameBaseNumber;

        private FileInfo _testableFileInfo;
        
        internal Task<OutputFile> MakeTestClassFile(FileInfo testableFileInfo)
        {
            return Task.Run(() =>
            {
                _testableFileInfo = testableFileInfo;
                return new OutputFile(MakeTestClassFileName(),
                    MakeTestClassFileContent());
            });
        }

        private string MakeTestClassFileName()
        {
            return _testableFileInfo.Namespaces[0].Classes[0].Name;
        }

        private string MakeTestClassFileContent()
        {
            _stackFrameBaseNumber = new StackTrace().FrameCount;
            
            AppendLine(@"using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using NUnit.Framework;
                using Moq;"
            );

            foreach (NamespaceInfo ns in _testableFileInfo.Namespaces)
            {
                AppendFormat("using {0};\n", ns.Name);
            }

            AppendLine();

            AppendFormat("namespace {0}.Tests\n", _testableFileInfo.Namespaces[0].Name);
            AppendLine("{");
            foreach (NamespaceInfo ns in _testableFileInfo.Namespaces)
            {
                foreach (TestableClassInfo ci in ns.Classes)
                {
                    AddTestClass(ci);
                }
            }

            AppendLine("}");

            return _sb.ToString();
        }

        private void AppendNoIndent(string str = "")
        {
            _sb.Append(str);
        }

        private void AppendLine(string str = "")
        {
            AddIndent();
            _sb.AppendLine(str);
        }

        private void AppendFormat(string format, params object[] args)
        {
            AddIndent();
            _sb.AppendFormat(format, args);
        }

        private void AddIndent()
        {
            int currDepth = new StackTrace().FrameCount;
            for (int i = 0; i < currDepth - 2 - _stackFrameBaseNumber; i++)
                _sb.Append(Tab);
        }

        private void AddTestClass(TestableClassInfo testableClassInfo)
        {
            AppendFormat("public class {0}Tests\n", testableClassInfo.Name);
            AppendLine("{");
            AddSetUp(testableClassInfo);
            foreach (BaseMethodInfo mi in testableClassInfo.Methods)
            {
                AddMethodTest(mi);
            }

            AppendLine("}");
        }

        private void AddSetUp(TestableClassInfo testableClassInfo)
        {
            var mockTypesByVarName = new List<KeyValuePair<string, string>>();

            _testObjectVarName = StringUtil.GetPrivateVarName(testableClassInfo.Name);

            AppendFormat("private {0} {1};\n", testableClassInfo.Name, _testObjectVarName);

            if (testableClassInfo.Constructors.Any())
                foreach (var kvp in testableClassInfo.Constructors[0]
                    .ParamTypeNamesByParamName)
                {
                    string varName = StringUtil.GetPrivateVarName(kvp.Key);
                    if (kvp.Value[0] == 'I')
                    {
                        AppendFormat("private Mock<{0}> {1};\n", kvp.Value, varName);
                        mockTypesByVarName.Add(new KeyValuePair<string, string>(varName, kvp.Value));
                    }
                    else
                    {
                        string fullTypeName = StringUtil.GetFullTypeName(kvp.Value);
                        System.Type type = System.Type.GetType(fullTypeName);
                        object defaultValue = TypeUtil.GetDefault(type);
                        
                        AppendFormat("private {0} {1} = {2};\n",
                            kvp.Value,
                            StringUtil.GetPrivateVarName(kvp.Key),
                            defaultValue ?? "null");
                    }
                }

            AppendLine();

            AppendLine("[SetUp]");
            AppendLine("public void SetUp()");
            AppendLine("{");

            AddSetUpArrange(testableClassInfo, mockTypesByVarName);

            AppendLine("}");

            AppendLine();
        }

        private void AddSetUpArrange(TestableClassInfo classInfo, List<KeyValuePair<string, string>> mockTypesByVarName)
        {
            if (!classInfo.Constructors.Any())
                return;

            foreach (var kvp in mockTypesByVarName)
            {
                AppendFormat("{0} = new Mock<{1}>();\n", kvp.Key, kvp.Value);
            }

            AppendFormat("{0} = new {1}(", _testObjectVarName, classInfo.Name);

            List<KeyValuePair<string, string>> paramPairs = classInfo
                .Constructors[0]
                .ParamTypeNamesByParamName;
            for (int i = 0; i < paramPairs.Count - 1; i++)
            {
                AppendNoIndent(StringUtil.GetPrivateVarName(paramPairs[i].Key) + ".Object, ");
            }

            if (paramPairs.Count > 0)
            {
                AppendNoIndent(StringUtil.GetPrivateVarName(paramPairs[paramPairs.Count - 1].Key));
            }

            AppendNoIndent(");\n");
        }

        private void AddMethodTest(BaseMethodInfo methodInfo)
        {
            AppendLine("[Test]");
            AppendFormat("public void {0}Test()\n", methodInfo.Name);

            AppendLine("{");
            AddMethodTestArrange(methodInfo);
            AddMethodTestAct(methodInfo);
            AddMethodTestAssert(methodInfo);
            AppendLine("}");

            AppendLine();
        }

        private void AddMethodTestArrange(BaseMethodInfo methodInfo)
        {
            foreach (var kvp in methodInfo.ParamTypeNamesByParamName)
            {
                string fullTypeName = StringUtil.GetFullTypeName(kvp.Value);
                System.Type type = System.Type.GetType(fullTypeName);
                object defaultValue = TypeUtil.GetDefault(type);
                AppendFormat("{0} {1} = {2};\n",
                    kvp.Value,
                    kvp.Key,
                    defaultValue ?? "null");
            }

            AppendLine();
        }
        
        private void AddMethodTestAct(BaseMethodInfo methodInfo)
        {
            if (methodInfo.ReturnTypeName != "void")
            {
                AppendFormat("{0} actual = ", methodInfo.ReturnTypeName);
                AppendNoIndent(_testObjectVarName + "." + methodInfo.Name + "(");
            }
            else
                AppendFormat("{0}", _testObjectVarName + "." + methodInfo.Name + "(");

            List<KeyValuePair<string, string>> paramPairs = methodInfo.ParamTypeNamesByParamName;
            for (int i = 0; i < paramPairs.Count - 1; i++)
            {
                AppendNoIndent(paramPairs[i].Key + ", ");
            }

            if (paramPairs.Count > 0)
            {
                AppendNoIndent(paramPairs[paramPairs.Count - 1].Key);
            }

            AppendNoIndent(");\n");

            AppendLine();
        }

        private void AddMethodTestAssert(BaseMethodInfo methodInfo)
        {
            if (methodInfo.ReturnTypeName != "void")
            {
                string fullTypeName = StringUtil.GetFullTypeName(methodInfo.ReturnTypeName);
                System.Type type = System.Type.GetType(fullTypeName);
                object defaultValue = TypeUtil.GetDefault(type);

                AppendFormat("{0} expected = {1};\n", methodInfo.ReturnTypeName, defaultValue);
                AppendLine("Assert.That(actual, Is.EqualTo(expected));");
            }

            AppendLine("Assert.Fail(\"autogenerated\");");
        }
    }
}