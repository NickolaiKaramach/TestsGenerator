namespace TestGeneratorLib.Util
{
    public static class StringUtil
    {
        public static string GetFullTypeName(string name)
        {
            string result;
            name = name.Trim();
            switch (name)
            {
                case "bool":
                    result = "System.Boolean";
                    break;

                case "byte":
                    result = "System.Byte";
                    break;

                case "sbyte":
                    result = "System.SByte";
                    break;

                case "short":
                    result = "System.Int16";
                    break;

                case "ushort":
                    result = "System.UInt16";
                    break;

                case "int":
                    result = "System.Int32";
                    break;

                case "uint":
                    result = "System.UInt32";
                    break;

                case "ulong":
                    result = "System.UInt64";
                    break;

                case "float":
                    result = "System.Single";
                    break;

                case "decimal":
                    result = "System.Decimal";
                    break;

                case "char":
                    result = "System.Char";
                    break;

                case "string":
                    result = "System.String";
                    break;

                case "object":
                    result = "System.Object";
                    break;

                default:
                    result = "";
                    break;
            }

            return result;
        }

        public static string GetPrivateVarName(string className)
        {
            return "_" + className[0].ToString().ToLower() + className.Substring(1);
        }
    }
}