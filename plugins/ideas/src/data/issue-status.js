//
// Copyright (c) City of Edmonton. All rights reserved.
// Licensed under GPL. See LICENSE file in the project root for full license information.
// @author j-rewerts
//

const statusMap = {
  'New': {
    display: 'New',
    class: 'issue-new',
    amount: 5
  },
  'Assigned': {
    display: 'Assigned',
    class: 'issue-assigned',
    amount: 25
  },
  'InProgress': {
    display: 'In Progress',
    class: 'issue-inprogress',
    amount: 50
  },
  'Pending': {
    display: 'Pending',
    class: 'issue-pending',
    amount: 75
  },
  'Resolved': {
    display: 'Resolved',
    class: 'issue-resolved',
    amount: 95
  },
  'Closed': {
    display: 'Closed',
    class: 'issue-closed',
    amount: 100
  },
  'Cancelled': {
    display: 'Cancelled',
    class: 'issue-cancelled',
    amount: 5
  },
  'Rejected': {
    display: 'Rejected',
    class: 'issue-rejected',
    amount: 5
  },
  'WaitingApproval': {
    display: 'WaitingApproval',
    class: 'issue-waitingApproval',
    amount: 80
  },
  'Planning': {
    display: 'Planning',
    class: 'issue-planning',
    amount: 85
  },
  'Completed': {
    display: 'Completed',
    class: 'issue-completed',
    amount: 95
  }

}

/**
 * Converts the web api status to a display status. Meant to be used as a Vue filter.
 * @param {String} status The string from the Octava web api.
 * @returns {String} The display status.
 */
function displayStatus (status) {
  return statusMap[status].display
}

/**
 * Gets the class associated with this status.
 * This isn't the best way to do this, but it makes changes convenient and understandeable.
 * @param {String} status The string from the Octava web api.
 * @returns {String} The class of the status.
 */
function statusClass (status) {
  return statusMap[status].class
}

/**
 * Gets the amount to completion (out of 100) this status represents.
 * @param {String} status The string from the Octava web api.
 * @returns {Number} The amount of completion.
 */
function statusAmount (status) {
  return statusMap[status].amount
}

export { displayStatus, statusClass, statusAmount }
