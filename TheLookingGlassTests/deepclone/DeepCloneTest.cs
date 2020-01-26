using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheLookingGlass.DeepClone;

namespace TheLookingGlassTests.DeepClone
{
    [TestClass]
    public class DeepCloneTest
    {

        public class NestTest {
            public object[] ObjectArray;
            public int AnInt;
        }

        [TestMethod]
        public void DeepClone_ClonesUserDefinedType()
        {
            IList<NestTest> a = new List<NestTest>
            {
                new NestTest { AnInt = 1 }
            };
            var dict = new Dictionary<int, int> {{ 3, 10 }};
            a[0].ObjectArray = new object[] {dict};
            
            var b = a.DeepClone();

            Assert.AreEqual(a.Count, b.Count);
            Assert.AreEqual(a[0].AnInt, b[0].AnInt);
            Assert.AreEqual(((IDictionary<int, int>) a[0].ObjectArray[0]).Count,
                ((IDictionary<int, int>) b[0].ObjectArray[0]).Count);
            Assert.AreEqual(((IDictionary<int, int>)a[0].ObjectArray[0])[3],
                ((IDictionary<int, int>)b[0].ObjectArray[0])[3]);
        }
    }
}
 