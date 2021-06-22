using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Options;

namespace ZooKeeprDistributedLockV2
{
    public class ZooKeeprOptions : IOptions<ZooKeeprOptions>
    {
        internal const string OptionKey = "ZooKeeprDistributedLock";

        public string ConnectionString { get; set; }

        public ZooKeeprOptions Value => this;
    }
}
