/**
 *
 * Exports some functions that loaded by host (.NET program) dynamically.
 * plugin_compute function acts like the long-running operation via sleep.
 */

#include <stdint.h>
#include <string.h>

#ifdef _WIN32
  #include <windows.h>
  #define EXPORT __declspec(dllexport)
#else
  #include <unistd.h>
  #define EXPORT __attribute__((visibility("default")))
#endif

EXPORT int32_t plugin_version(void)
{
    return 1;
}

EXPORT int64_t plugin_compute(int64_t a, int64_t b)
{
#ifdef _WIN32
    Sleep(2000);
#else
    sleep(2);
#endif
    return a + b;
}

EXPORT int32_t plugin_greet(const char *name, char *out_buf, int32_t buf_len)
{
    if (!name || !out_buf || buf_len <= 0)
        return -1;

    /* returns "Hello, <name>!" */
    const char prefix[] = "Hello, ";
    const char suffix[] = "!";
    int32_t need = (int32_t)(strlen(prefix) + strlen(name) + strlen(suffix) + 1);

    if (need > buf_len)
        return -2; /* buffer too small */

    char *p = out_buf;
    for (const char *s = prefix; *s; ++s) *p++ = *s;
    for (const char *s = name;   *s; ++s) *p++ = *s;
    for (const char *s = suffix; *s; ++s) *p++ = *s;
    *p = '\0';

    return need - 1; /* length without null terminator */
}
