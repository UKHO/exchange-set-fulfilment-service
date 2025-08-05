import { sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

import { 
  getSmallJobFilter, 
  getMediumJobFilter 
} from './Helper/DataHelper.js';
import { createAndMonitorJob } from './Scripts/LoadTestForJobCreation.js';
import { authenticateUsingAzure } from './Oauth/Azure.js';
const config = JSON.parse(open('./config.json'));

export let options = {
  scenarios: {
    // Small jobs - high volume
    SmallJobCreation: {
      exec: 'createSmallJob',
      executor: 'per-vu-iterations',
      startTime: '5s',
      gracefulStop: '5s',
      vus: 2,
      iterations: 5,
      maxDuration: '5m'
    },

    // Medium jobs - moderate volume
    MediumJobCreation: {
      exec: 'createMediumJob',
      executor: 'per-vu-iterations',
      startTime: '30s',
      gracefulStop: '5s',
      vus: 2,
      iterations: 5,
      maxDuration: '5m'
    }
  }
};

export function setup() {
  console.log("Starting load test for Exchange Set Fulfillment Service");
  console.log("Test start time: " + new Date().toISOString());
  return {};
}

export function Authsetup() {
    // client credentials authentication flow
    let efsAuthResp = authenticateUsingAzure(
        `${config.EFS_TENANT_ID}`, `${config.EFS_CLIENT_ID}`, `${config.EFS_SCOPES}`, `${config.EFS_RESOURCE}`
    );
    clientAuthResp["efsToken"] = efsAuthResp.access_token;

    return clientAuthResp;
}

export function createSmallJob() {
  const filter = getSmallJobFilter();
  const result = createAndMonitorJob(filter, 'Small');

  console.log(`Small job ${result.jobId} completed with status: ${result.status}`);
  if (result.status === 'success') {
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${result.buildTime.toFixed(2)}ms`);
  }

  sleep(1);
}

export function createMediumJob() {
  const filter = getMediumJobFilter();
  const result = createAndMonitorJob(filter, 'Medium');

  console.log(`Medium job ${result.jobId} completed with status: ${result.status}`);
  if (result.status === 'success') {
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${result.buildTime.toFixed(2)}ms`);
  }

  sleep(1);
}

export function handleSummary(data) {
  return {
    ["summary/JobCreationResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["summary/JobCreationResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}