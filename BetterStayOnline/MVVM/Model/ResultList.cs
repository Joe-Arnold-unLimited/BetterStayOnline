using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline.MVVM.Model
{
    class ResultList
    {
        private List<BandWidthTest> testResults = new List<BandWidthTest>();

        struct BandWidthTest
        {
            public DateTime date;
            public double downSpeed;
            public double upSpeed;
        }

        public void AddResult(DateTime time, double downSpeed, double upSpeed)
        {
            BandWidthTest newTest = new BandWidthTest();

            newTest.date = time;
            newTest.downSpeed = downSpeed;
            newTest.upSpeed = upSpeed;

            AddResult(newTest);
        }

        private void AddResult(BandWidthTest testResult)
        {
            testResults.Add(testResult);
            testResults.OrderBy(x => x.date);
        }
    }
}
