namespace Echoes.Cli
{
    public class StatViewModel
    {
        public int PlainRequestCount { get; set; }  

        public long PlainRequestSize { get; set; }

        public int PlainResponseCount { get; set; } 

        public long PlainResponseSize { get; set; }

        public int SecureRequestCount { get; set; }

        public long SecureRequestSize { get; set; }

        public int SecureResponseCount { get; set; }

        public long SecureResponseSize { get; set; }


        public int TotalRequest
        {
            get
            {
                return PlainRequestCount + SecureRequestCount; 
            }
        }

        public long TotalRequestSize
        {
            get
            {
                return SecureRequestSize + PlainRequestSize;
            }
        }

        public int TotalResponse
        {
            get
            {
                return PlainResponseCount + SecureResponseCount;
            }
        }

        public long TotalResponseSize
        {
            get
            {
                return PlainResponseSize + SecureResponseSize; 
            }
        }

        //public string BoundAddress { get; set;  }

        //public int BoundPort { get; set; } 
        
        public string BoundPointsDescription { get; set; }
    }

}