using System;
using System.Text;

namespace UnitTest.Files
{
    public class DependentClass
    {
        public DependentClass(IDisposable disp, IFormattable formattable, int i)
        {
        }

        public DependentClass(int i)
        {
        }

        public void VoidMethod(string myStr, Decoder decoder)
        {
        }

        public int IntMethod(int i)
        {
            return 42;
        }
    }
}