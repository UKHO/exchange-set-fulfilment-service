import { sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

import { 
  getSmallJobFilter, 
  getMediumJobFilter 
} from './Helper/DataHelper.js';
import { create, status, build } from './Scripts/LoadTestForJobCreation.js';
import { authenticateUsingAzure } from './Oauth/Azure.js';
const config = JSON.parse(open('./config.json'));

// Define request counts that can be easily adjusted for future cycles
const CYCLE = {
  SMALL_JOBS: 950,   // 95% of 1000
  MEDIUM_JOBS: 50,   // 5% of 1000
  STATUS_CHECKS: 1000,
  BUILD_CHECKS: 1000
};

// Test duration in seconds (1 hour)
const TEST_DURATION = 3600;

export let options = {
  scenarios: {
    // Small jobs (95% of job creation requests)
    SmallJobCreation: {
      executor: 'constant-arrival-rate',
      exec: 'createSmallJob',
      rate: CYCLE.SMALL_JOBS / TEST_DURATION,
      timeUnit: '1s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 5,
      maxVUs: 16,
      startTime: '0s',
      gracefulStop: '30s',
    },

    // Medium jobs (5% of job creation requests)
    MediumJobCreation: {
      executor: 'constant-arrival-rate',
      exec: 'createMediumJob',
      rate: CYCLE.MEDIUM_JOBS / TEST_DURATION,
      timeUnit: '1s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 1,
      maxVUs: 2,
      startTime: '30s',
      gracefulStop: '30s',
    },

    //  Job status checks
    JobStatus: {
      executor: 'constant-arrival-rate',
      exec: 'getStatusOfJob',
      rate: CYCLE.STATUS_CHECKS / TEST_DURATION,
      timeUnit: '1s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 5,
      maxVUs: 16,
      startTime: '120s', // Start after some jobs have been created
      gracefulStop: '30s',
    },

    // Build job status checks
    BuildJobStatus: {
      executor: 'constant-arrival-rate',
      exec: 'getBuildStatusOfJob',
      rate: CYCLE.BUILD_CHECKS / TEST_DURATION,
      timeUnit: '1s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 5,
      maxVUs: 16,
      startTime: '120s', // Start after some jobs have been created
      gracefulStop: '30s',
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
  const result = create(filter, 'Small');

  if (result.status === 'success') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
  }

  sleep(1);
}

export function createMediumJob() {
  const filter = getMediumJobFilter();
  const result = create(filter, 'Medium');

  if (result.status === 'success') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
  }

  sleep(1);
}

export function getStatusOfJob() {
  const Id = "job-small-1754999159418-27a5dfbfd1024de5";
  const result = status(Id);

  if (result.status === 'Completed') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
  }

  sleep(1);
}

export function getBuildStatusOfJob() {
  const Id = "job-small-1754999159418-27a5dfbfd1024de5";
  const result = build(Id);

  if (result.status === 'Success') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
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
