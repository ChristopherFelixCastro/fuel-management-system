import type { AuthSession } from '../types/auth'

interface AuthenticatedFetchOptions {
  session: AuthSession
  refreshSession: () => Promise<AuthSession | null>
  input: RequestInfo | URL
  init?: RequestInit
}

function withAuthorization(
  init: RequestInit | undefined,
  accessToken: string,
): RequestInit {
  const headers = new Headers(init?.headers)

  headers.set(
    'Authorization',
    `Bearer ${accessToken}`,
  )

  return {
    ...init,
    headers,
  }
}

export async function authenticatedFetch({
  session,
  refreshSession,
  input,
  init,
}: AuthenticatedFetchOptions): Promise<Response> {
  let response = await fetch(
    input,
    withAuthorization(
      init,
      session.accessToken,
    ),
  )

  if (response.status !== 401) {
    return response
  }

  const renewedSession = await refreshSession()

  if (!renewedSession) {
    return response
  }

  response = await fetch(
    input,
    withAuthorization(
      init,
      renewedSession.accessToken,
    ),
  )

  return response
}