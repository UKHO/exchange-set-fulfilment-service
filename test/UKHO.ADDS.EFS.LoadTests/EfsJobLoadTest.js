import { sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { Counter } from 'k6/metrics';

import { getSmallJobFilter, getMediumJobFilter } from './Services/JobFilterProvider.js';
import { create, status, build } from './Services/JobScenarios.js';
import { authenticateUsingAzure } from './Oauth/Azure.js';
const config = JSON.parse(open('./config.json'));

// Custom counters for tracking job creation requests
const smallJobCounter = new Counter('small_job_requests');
const mediumJobCounter = new Counter('medium_job_requests');

const totalRequests = config.NumberOfRequests; // Total requests for the entire test duration

// Define request counts that can be easily adjusted for future cycles
const CYCLE = {
  SMALL_JOBS: totalRequests * 0.95,   // 95% of 1000
  MEDIUM_JOBS: totalRequests * 0.05,   // 5% of 1000
  STATUS_CHECKS: totalRequests,
  BUILD_CHECKS: totalRequests
};

// Test duration in seconds
const TEST_DURATION = config.DurationInSeconds;

export let options = {
  scenarios: {
    // Small jobs (95% of job creation requests)
    SmallJobCreation: {
      executor: 'constant-arrival-rate',
      exec: 'createSmallJob',
      rate: Math.ceil(CYCLE.SMALL_JOBS / (TEST_DURATION / 120)),
      timeUnit: '120s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 5,
      maxVUs: 50,
      startTime: '0s',
      gracefulStop: '30s'
    },

    // Medium jobs (5% of job creation requests)
    MediumJobCreation: {
      executor: 'constant-arrival-rate',
      exec: 'createMediumJob',
      rate: Math.ceil(CYCLE.MEDIUM_JOBS / (TEST_DURATION / 120)),
      timeUnit: '120s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 1,
      maxVUs: 2,
      startTime: '5s',
      gracefulStop: '30s'
    },

    //  Job status checks
    JobStatus: {
      executor: 'constant-arrival-rate',
      exec: 'getStatusOfJob',
      rate: Math.ceil(CYCLE.STATUS_CHECKS / (TEST_DURATION / 120)),
      timeUnit: '120s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 5,
      maxVUs: 16,
      startTime: '30s', // Start after some jobs have been created
      gracefulStop: '30s'
    },

    // Build job status checks
    BuildJobStatus: {
      executor: 'constant-arrival-rate',
      exec: 'getBuildStatusOfJob',
      rate: Math.ceil(CYCLE.BUILD_CHECKS / (TEST_DURATION / 120)),
      timeUnit: '120s',
      duration: `${TEST_DURATION}s`,
      preAllocatedVUs: 5,
      maxVUs: 16,
      startTime: '30s', // Start after some jobs have been created
      gracefulStop: '30s'
    }
  }
};

// export function setup() {
//   // client credentials authentication flow
//   let efsAuthResp = authenticateUsingAzure(
//     `${config.EFS_TENANT_ID}`, `${config.EFS_CLIENT_ID}`, `${config.EFS_CLIENT_SECRET}`, `${config.EFS_SCOPES}`, `${config.EFS_RESOURCE}`
//   );
//   let clientAuthResp = {}; // Define the object
//   clientAuthResp["efsToken"] = efsAuthResp.access_token;

//   return clientAuthResp;
// }

export function createSmallJob() {
  smallJobCounter.add(1);
  const filter = getSmallJobFilter();
  const result = create(filter, 'Small');

  if (result.status === 'success') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
  }

  sleep(1);
}

export function createMediumJob() {
  mediumJobCounter.add(1);
  const filter = getMediumJobFilter();
  const result = create(filter, 'Medium');

  if (result.status === 'success') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
  }

  sleep(1);
}

export function getStatusOfJob() {
  const Id = config.CompletedJobId;
  const result = status(Id);

  if (result.status === 'Completed') {
    const buildTimeStr = (typeof result.buildTime === 'number' && !isNaN(result.buildTime)) ? result.buildTime.toFixed(2) : 'N/A';
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${buildTimeStr}ms`);
  }

  sleep(1);
}

export function getBuildStatusOfJob() {
  const Id = config.CompletedJobId;
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
