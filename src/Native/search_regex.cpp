// Copyright 2014 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

#include "stdafx.h"

#include <regex>

#include "search_regex.h"

RegexSearch::RegexSearch()
    : pattern_(NULL),
      patternLen_(0),
      regex_(nullptr) {
}

RegexSearch::~RegexSearch() {
  delete regex_;
}

bool RegexSearch::PreProcess(const char *pattern, int patternLen, SearchOptions options) {
  auto flags = std::regex::ECMAScript;
  if ((options & kMatchCase) == 0) {
    flags = flags | std::regex::icase;
  }
  regex_ = new std::regex(pattern, patternLen, flags);
  pattern_ = pattern;
  patternLen_ = patternLen;
  return true;
}

int RegexSearch::GetSearchBufferSize() {
  return sizeof(std::cregex_iterator);
}

void RegexSearch::Search(SearchParams* searchParams) {
  std::cregex_iterator end;
  std::cregex_iterator* pit =
      reinterpret_cast<std::cregex_iterator *>(searchParams->SearchBuffer);
  // Placement new for the iterator on the 1st call
  if (searchParams->MatchStart == nullptr) {
    pit = new(pit) std::cregex_iterator(
        searchParams->TextStart,
        searchParams->TextStart + searchParams->TextLength,
        *regex_);
  }
  // Iterate
  std::cregex_iterator& it(*pit);
  if (it == end) {
    // Explicit call to destructor on last call.
    searchParams->MatchStart = nullptr;
    pit->std::cregex_iterator::~cregex_iterator();
    return;
  }
  // Set result if match found
  searchParams->MatchStart = searchParams->TextStart + it->position();
  searchParams->MatchLength = (int)it->length(); // TODO(rpaquay): 2GB limit
  ++it;
}
