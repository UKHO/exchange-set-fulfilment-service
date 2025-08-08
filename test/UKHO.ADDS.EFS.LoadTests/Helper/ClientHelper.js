import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('../Config.json'));

// Trends for tracking job metrics
export const SmallJobCreateResponseTime = new Trend('SmallJobCreateResponseTime');
export const MediumJobCreateResponseTime = new Trend('MediumJobCreateResponseTime');

export const SmallJobDurationTime = new Trend('SmallJobDurationTime');
export const MediumJobDurationTime = new Trend('MediumJobDurationTime');

export const SmallJobStatusResponseTime = new Trend('SmallJobStatusResponseTime');
export const MediumJobStatusResponseTime = new Trend('MediumJobStatusResponseTime');

// Trends for tracking API response times
export const JobCreateResponseTime = new Trend('JobCreateResponseTime');
export const JobStatusResponseTime = new Trend('JobStatusResponseTime');
export const JobDurationTime = new Trend('JobDurationTime');

/**
* Creates a job with the given filter
* @param {string} filter - The filter to use for job creation
* @param {string} jobSize - The size category of the job (Small, Medium)
* @returns {Object} - Response object with job details
*/
export function createJob(filter, jobSize) {
  const correlationId = generateCorrelationId(jobSize);
  const payload = JSON.stringify({
    version: 1,
    dataStandard: "s100",
    products: "",
    filter: filter
  });

  const url = `${config.Base_URL}${config.JobsEndpoint}`;

  const params = {
    headers: {
      'X-Correlation-Id': correlationId,
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
* @param {string} jobId - The ID of the job
* @returns {Object} - Response object with job status
* @param {string} jobSize - The size category of the job (Small, Medium)
*/
export function getJobStatus(jobId, jobSize) {
  const url = `${config.Base_URL}${config.JobStatusEndpoint.replace('{jobId}', jobId)}`;

  const params = {
    headers: {
      'Content-Type': 'application/json'
    }
  };

  const response = http.get(url, params);

  check(response, {
    'Job status retrieved successfully': (r) => r.status === 200
  });

  JobStatusResponseTime.add(response.timings.duration);

  // Record job creation time
  switch(jobSize) {
    case 'Small':
      SmallJobStatusResponseTime.add(response.timings.duration);
      break;
    case 'Medium':
      MediumJobStatusResponseTime.add(response.timings.duration);
      break;
  }

  return {
    status: response.json().jobState,
    buildStatus: response.json().buildState,
    response: response
  };
}

/**
* Generates a unique correlation ID
* @param {string} prefix - Prefix for the correlation ID
* @returns {string} - Generated correlation ID
*/
export function generateCorrelationId(prefix) {
  // Format: job-[size]-[timestamp with ms]-[random hex]
  const timestamp = new Date().getTime();

  // Generate 8 bytes (16 hex chars) of randomness
  const random = Array.from(
    { length: 16 }, 
    () => Math.floor(Math.random() * 16).toString(16)
  ).join('');

  return `job-${prefix.toLowerCase()}-${timestamp}-${random}`;
}
