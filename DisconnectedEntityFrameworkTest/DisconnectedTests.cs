using System;
using System.Linq;
using DisconnectedEntityFramework;
using NUnit.Framework;

namespace DisconnectedEntityFrameworkTest
{
    [TestFixture]
    public class DisconnectedTests
    {
        [Test]
        public void Test()
        {
            var context = new SimpleContext();

            foreach (var parentEntity in context.ParentEntities.ToList())
            {
                Console.WriteLine(parentEntity.Name);
            }
        }
    }
}
