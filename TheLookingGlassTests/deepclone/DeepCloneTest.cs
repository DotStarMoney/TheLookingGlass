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
            public object[] objectArray;
            public int anInt;
        }

        [TestMethod]
        public void DeepClone_ClonesUserDefinedType()
        {
            IList<NestTest> a = new List<NestTest>
            {
                new NestTest { anInt = 1 }
            };
            a[0].objectArray = new object[] { new Dictionary<int, int>() };
            
            IList<NestTest> b = a.DeepClone();

            NestTest d = new NestTest();
            string ds = d.GetType().GetRuntimeFields().ToString();

        }
    }
}
 