using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cities_States.Controllers
{
    public class NotificationHub : Hub
    {
        public void NotifyNewEmployee(string employeeName)
        {
            Clients.All.newEmployeeAdded(employeeName);
        }
    }
}