using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BandProgram
{
    class Global
    {
        private static Global m_instance = null;
        public static Global Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new Global();
                return m_instance;
            }
        }
        public Global()
        {
            m_instance = this;
        }


        public bool isTest = false;
        public bool isLocalLogin = false;
    }
}
