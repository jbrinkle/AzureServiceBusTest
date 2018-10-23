using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Vivint.ServiceBus.RequestResponse;

namespace SBRRTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private RequestResponseFactory factory;

        public ValuesController(RequestResponseFactory factory)
        {
            this.factory = factory;
        }

        // GET api/values
        [HttpPost]
        public async Task<ActionResult<GetLoanOptionsResponsePayload>> Post(GetLoanOptionsRequestPayload request)
        {
            var sender = factory.GetSender<GetLoanOptionsRequestPayload, GetLoanOptionsResponsePayload>();

            var response = await sender.SendRequest(request);

            return response;
        }

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public ActionResult<string> Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
