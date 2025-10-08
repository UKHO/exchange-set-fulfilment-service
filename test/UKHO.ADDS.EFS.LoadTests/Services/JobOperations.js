import http from 'k6/http';
import { check } from 'k6';
import { Trend } from 'k6/metrics';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const config = JSON.parse(open('../Config.json'));

// Trends for tracking job metrics
export const SmallJobCreateResponseTime = new Trend('SmallJobCreateResponseTime');
export const MediumJobCreateResponseTime = new Trend('MediumJobCreateResponseTime');

// Trends for tracking API response times
export const JobCreateResponseTime = new Trend('JobCreateResponseTime');
export const JobStatusResponseTime = new Trend('JobStatusResponseTime');
export const JobBuildResponseTime = new Trend('JobBuildResponseTime');

/**
* Creates a job with the given filter
* @param {string} filter - The filter to use for job creation
* @param {string} jobSize - The size category of the job (Small, Medium)
* @returns {Object} - Response object with job details
*/
export function createJob(filter, jobSize) {
  const correlationId = generateCorrelationId();
  const payload = JSON.stringify({
    dataStandard: "s100",
    products: [""],
    filter: filter
  });

  const url = `${config.Base_URL}${config.JobsEndpoint}`;

  const params = {
    headers: {
      'X-Correlation-Id': correlationId,
      'Authorization': `Bearer ${config.EFSToken}`,
      'Content-Type': 'application/json'
    }
  };

  const response = http.post(url, payload, params);

  check(response, {
    'Job created successfully': (r) => r.status === 200,
    'Job ID is present': (r) => r.json().jobId !== undefined
  });

  JobCreateResponseTime.add(response.timings.duration);

  // Record job creation time
  switch(jobSize) {
    case 'Small':
      SmallJobCreateResponseTime.add(response.timings.duration);
      break;
    case 'Medium':
      MediumJobCreateResponseTime.add(response.timings.duration);
      break;
  }

  return {
    jobId: response.json().jobId,
    correlationId: correlationId,
    response: response
  };
}

/**
* Gets the status of a job
* @param {string} Id - The ID of the job
* @returns {Object} - Response object with job status
*/
export function getJobStatus(Id = "11b1e20d-188c-1d41-b2f7-87ff4674c175") {
  const url = `${config.Base_URL}${config.JobStatusEndpoint.replace('{jobId}', Id)}`;

  const params = {
    headers: {
      'Authorization': `Bearer ${config.EFSToken}`,
      'Content-Type': 'application/json'
    }
  };

  const response = http.get(url, params);

  JobStatusResponseTime.add(response.timings.duration);

  return {
    response: response
  };
}

/**
* Gets the build details of a job
* @param {string} Id - The ID of the job
* @returns {Object} - Response object with build details
*/
export function getJobBuild(Id = "11b1e20d-188c-1d41-b2f7-87ff4674c175") {
  const url = `${config.Base_URL}${config.JobBuildEndpoint.replace('{jobId}', Id)}`;

  const params = {
    headers: {
      'Authorization': `Bearer ${config.EFSToken}`,
      'Content-Type': 'application/json'
    }
  };

  const response = http.get(url, params);

  JobBuildResponseTime.add(response.timings.duration);
  
  return {
    response: response
  };
}

export function generateCorrelationId() {
  function s4() {
    return randomIntBetween(0, 0xFFFF).toString(16).padStart(4, '0');
  }
  return `${s4()}${s4()}-${s4()}-${s4()}-${s4()}-${s4()}${s4()}${s4()}`;
}
