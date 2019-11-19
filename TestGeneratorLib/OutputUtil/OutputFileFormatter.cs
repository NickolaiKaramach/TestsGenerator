using System;
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
        private readonly StringBuilder _sb = new StringBuilder(StringBuilderInitCapacity);

        private int _stackFrameBaseNumber;

        private FileInfo _testableFileInfo;
        private string _testObjectVarName;

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

            AppendLine(@"using System;");
            AppendLine(@"using System.Collections.Generic;");
            AppendLine(@"using System.Linq;");
            AppendLine(@"using System.Text;");
            AppendLine(@"using NUnit.Framework;");
            AppendLine(@"using Moq;");

            foreach (var namespaceInfo in _testableFileInfo.Namespaces)
            {
                AppendFormat("using {0};\n", namespaceInfo.Name);
            }

            AppendLine();

            AppendFormat("namespace {0}.Tests\n", _testableFileInfo.Namespaces[0].Name);
            AppendLine("{");
            foreach (var testableClassInfo in _testableFileInfo.Namespaces.SelectMany(namespaceInfo =>
                namespaceInfo.Classes))
            {
                AddTestClass(testableClassInfo);
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
            var currDepth = new StackTrace().FrameCount;
            for (var i = 0; i < currDepth - 2 - _stackFrameBaseNumber; i++)
            {
                _sb.Append(Tab);
            }
        }

        private void AddTestClass(TestableClassInfo testableClassInfo)
        {
            AppendFormat("public class {0}Tests\n", testableClassInfo.Name);
            AppendLine("{");
            AddSetUp(testableClassInfo);

            foreach (var methodInfo in testableClassInfo.Methods)
            {
                AddMethodTest(methodInfo);
            }

            AppendLine("}");
        }

        private void AddSetUp(TestableClassInfo testableClassInfo)
        {
            var mockTypesByVarName = new List<KeyValuePair<string, string>>();

            _testObjectVarName = StringUtil.GetPrivateVarName(testableClassInfo.Name);

            AppendFormat("private {0} {1};\n", testableClassInfo.Name, _testObjectVarName);

            if (testableClassInfo.Constructors.Any())
            {
                AddConstructors(testableClassInfo, mockTypesByVarName);
            }

            AppendLine();

            AppendLine("[SetUp]");
            AppendLine("public void SetUp()");
            AppendLine("{");

            AddSetUpArrange(testableClassInfo, mockTypesByVarName);

            AppendLine("}");

            AppendLine();
        }

        private void AddConstructors(TestableClassInfo testableClassInfo,
            ICollection<KeyValuePair<string, string>> mockTypesByVarName)
        {
            foreach (var (key, value) in testableClassInfo.Constructors[0]
                .ParamTypeNamesByParamName)
            {
                var varName = StringUtil.GetPrivateVarName(key);
                if (value[0] == 'I')
                {
                    AppendFormat("private Mock<{0}> {1};\n", value, varName);
                    mockTypesByVarName.Add(new KeyValuePair<string, string>(varName, value));
                }
                else
                {
                    var fullTypeName = StringUtil.GetFullTypeName(value);
                    var type = Type.GetType(fullTypeName);
                    var defaultValue = TypeUtil.GetDefault(type);

                    AppendFormat("private {0} {1} = {2};\n",
                        value,
                        StringUtil.GetPrivateVarName(key),
                        defaultValue ?? "null");
                }
            }
        }

        private void AddSetUpArrange(TestableClassInfo classInfo,
            IEnumerable<KeyValuePair<string, string>> mockTypesByVarName)
        {
            if (!classInfo.Constructors.Any())
            {
                return;
            }

            foreach (var (key, value) in mockTypesByVarName) AppendFormat("{0} = new Mock<{1}>();\n", key, value);

            AppendFormat("{0} = new {1}(", _testObjectVarName, classInfo.Name);

            var paramPairs = classInfo
                .Constructors[0]
                .ParamTypeNamesByParamName;

            for (var i = 0; i < paramPairs.Count - 1; i++)
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
            foreach (var (key, value) in methodInfo.ParamTypeNamesByParamName)
            {
                var fullTypeName = StringUtil.GetFullTypeName(value);
                var type = Type.GetType(fullTypeName);
                var defaultValue = TypeUtil.GetDefault(type);

                AppendFormat("{0} {1} = {2};\n",
                    value,
                    key,
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
            {
                AppendFormat("{0}", _testObjectVarName + "." + methodInfo.Name + "(");
            }

            var paramPairs = methodInfo.ParamTypeNamesByParamName;
            for (var i = 0; i < paramPairs.Count - 1; i++)
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
                var fullTypeName = StringUtil.GetFullTypeName(methodInfo.ReturnTypeName);
                var type = Type.GetType(fullTypeName);
                var defaultValue = TypeUtil.GetDefault(type);

                AppendFormat("{0} expected = {1};\n", methodInfo.ReturnTypeName, defaultValue);
                AppendLine("Assert.That(actual, Is.EqualTo(expected));");
            }

            AppendLine("Assert.Fail(\"autogenerated\");");
        }
    }
}