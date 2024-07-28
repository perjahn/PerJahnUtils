using System;
using System.Xml.Linq;

namespace GenerateTransform
{
    class UnitTest
    {
        public int Test()
        {
            bool result1 = PathTest1();
            Console.WriteLine($"PathTest1: {result1}");

            bool result2 = PathTest2();
            Console.WriteLine($"PathTest2: {result2}");

            bool result3 = CompareTest1();
            Console.WriteLine($"CompareTest1: {result3}");

            bool result4 = CompareTest2();
            Console.WriteLine($"CompareTest2: {result4}");

            bool result5 = CompareTest3();
            Console.WriteLine($"CompareTest3: {result5}");

            bool result6 = CompareTest4();
            Console.WriteLine($"CompareTest4: {result6}");

            if (!result1 || !result2 || !result3 || !result4 || !result5 || !result6)
            {
                Console.WriteLine("Fail!");
                return 1;
            }
            else
            {
                Console.WriteLine("Success!");
                return 0;
            }
        }

        private bool PathTest1()
        {
            XDocument xdoc = new();
            XElement el1 = new("bbb");
            XElement el2 = new("aaa", el1);

            xdoc.Add(el2);

            return XmlHelper.GetElementPath(el1) == "/aaa/bbb";
        }

        private bool PathTest2()
        {
            XElement el1 = new("bbb");

            return XmlHelper.GetElementPath(el1) == "/aaa/bbb";
        }

        private bool CompareTest1()
        {
            XElement e1 = new("aaa");
            XElement e2 = new("aaa");

            XAttribute a1 = new("a", "1");
            XAttribute a2 = new("a", "1");
            XAttribute a3 = new("b", "2");
            XAttribute a4 = new("b", "2");

            e1.Add(a1);
            e1.Add(a3);

            e2.Add(a2);
            e2.Add(a4);

            return XmlHelper.AreEqual(e1, e2);
        }

        private bool CompareTest2()
        {
            XElement e1 = new("aaa");
            XElement e2 = new("aaa");

            XAttribute a1 = new("a", "1");
            XAttribute a2 = new("a", "1");
            XAttribute a3 = new("b", "2");
            XAttribute a4 = new("b", "2");

            e1.Add(a3);
            e1.Add(a1);

            e2.Add(a2);
            e2.Add(a4);

            return XmlHelper.AreEqual(e1, e2);
        }

        private bool CompareTest3()
        {
            XElement e1 = new("aaa");
            XElement e2 = new("aaa");

            XAttribute a1 = new("a", "1");
            XAttribute a2 = new("a", "1");
            XAttribute a3 = new("b", "1");
            XAttribute a4 = new("b", "2");

            e1.Add(a3);
            e1.Add(a1);

            e2.Add(a2);
            e2.Add(a4);

            return !XmlHelper.AreEqual(e1, e2);
        }

        private bool CompareTest4()
        {
            XElement e1 = new("aaa");
            XElement e2 = new("aaa");

            XAttribute a1 = new("a", "1");
            XAttribute a2 = new("a", "1");
            XAttribute a4 = new("b", "2");

            e1.Add(a1);

            e2.Add(a2);
            e2.Add(a4);

            return !XmlHelper.AreEqual(e1, e2);
        }
    }
}
