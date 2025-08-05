// filepath: UKHO.ADDS.EFS.LoadTests/TestCase/EFSStressTest.js
import { sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

import { 
  getSmallJobFilter, 
  getMediumJobFilter 
} from './Helper/DataHelper.js';
import { createAndMonitorJob } from './Scripts/LoadTestForJobCreation.js';

export let options = {
  scenarios: {
    // Stress test with 10,000 requests over 1 hour
    StressTest: {
      executor: 'ramping-arrival-rate',
      startRate: 10,   // starting at 10 iterations per minute
      timeUnit: '1m',  // 1 minute
      preAllocatedVUs: 100,
      maxVUs: 500,
      stages: [
        { duration: '10m', target: 50 },    // Ramp up to 50 iterations per minute over 10 minutes
        { duration: '20m', target: 100 },   // Ramp up to 100 iterations per minute over 20 minutes
        { duration: '20m', target: 200 },   // Ramp up to 200 iterations per minute over 20 minutes
        { duration: '10m', target: 0 }      // Ramp down to 0 iterations per minute over 10 minutes
      ]
    }
  }
};

export function setup() {
  console.log("Starting stress test for Exchange Set Fulfillment Service");
  console.log("Test start time: " + new Date().toISOString());
  return {};
}

export default function() {
  // Randomly select job size with weighted distribution
  // 90% small, 5% medium, 5% large
  const random = Math.random();
  let jobSize, filter;

  if (random < 0.9) {
    jobSize = 'Small';
    filter = getSmallJobFilter();
  } else if (random < 0.95) {
    jobSize = 'Medium';
    filter = getMediumJobFilter();
  }

  const result = createAndMonitorJob(filter, jobSize);

  console.log(`${jobSize} job ${result.jobId} completed with status: ${result.status}`);
  if (result.status === 'success') {
    console.log(`Total time: ${result.totalTime.toFixed(2)}s, Build time: ${result.buildTime.toFixed(2)}ms`);
  }

  // Add a small sleep to prevent CPU overload
  sleep(1);
}

export function handleSummary(data) {
  return {
    ["summary/StressTestResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["summary/StressTestResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}