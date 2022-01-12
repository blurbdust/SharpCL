using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SharpCL;
using System.Diagnostics;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Net.Http;
using System.Text;
//using System.Net.Http;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x410

namespace ImageDemo
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Context context;
        private CommandQueue commandQueue;
        private Dictionary<string, Kernel> kernels;

        private const string kernelsCode = @"
/*
                typedef struct {
                    uint v[4];
                } password_hash_t;

                typedef struct {
                    uint size_bytes;
                    char password[32];
                } password_t;

                typedef struct {
                    uint size;
                    char buffer[64];
                } buffer_t;

                #define F(x, y, z)			((z) ^ ((x) & ((y) ^ (z))))
                #define G(x, y, z)			((y) ^ ((z) & ((x) ^ (y))))
                #define H(x, y, z)			((x) ^ (y) ^ (z))
                #define I(x, y, z)			((y) ^ ((x) | ~(z)))

                // The MD5 transformation for all four rounds.
                #define STEP(f, a, b, c, d, x, t, s) \ 
                    (a) += f((b), (c), (d)) + (x) + (t); \
                    (a) = (((a) << (s)) | (((a) & 0xffffffff) >> (32 - (s)))); \
                    (a) += (b);

                #define GET(i) (key[(i)])

                static void md5_round(uint* internal_state, const uint* key) {
                    uint a, b, c, d;
                    a = internal_state[0];
                    b = internal_state[1];
                    c = internal_state[2];
                    d = internal_state[3];

                    // Round 1
                    STEP(F, a, b, c, d, GET(0), 0xd76aa478, 7)
                    STEP(F, d, a, b, c, GET(1), 0xe8c7b756, 12)
                    STEP(F, c, d, a, b, GET(2), 0x242070db, 17)
                    STEP(F, b, c, d, a, GET(3), 0xc1bdceee, 22)
                    STEP(F, a, b, c, d, GET(4), 0xf57c0faf, 7)
                    STEP(F, d, a, b, c, GET(5), 0x4787c62a, 12)
                    STEP(F, c, d, a, b, GET(6), 0xa8304613, 17)
                    STEP(F, b, c, d, a, GET(7), 0xfd469501, 22)
                    STEP(F, a, b, c, d, GET(8), 0x698098d8, 7)
                    STEP(F, d, a, b, c, GET(9), 0x8b44f7af, 12)
                    STEP(F, c, d, a, b, GET(10), 0xffff5bb1, 17)
                    STEP(F, b, c, d, a, GET(11), 0x895cd7be, 22)
                    STEP(F, a, b, c, d, GET(12), 0x6b901122, 7)
                    STEP(F, d, a, b, c, GET(13), 0xfd987193, 12)
                    STEP(F, c, d, a, b, GET(14), 0xa679438e, 17)
                    STEP(F, b, c, d, a, GET(15), 0x49b40821, 22)

                    // Round 2
                    STEP(G, a, b, c, d, GET(1), 0xf61e2562, 5)
                    STEP(G, d, a, b, c, GET(6), 0xc040b340, 9)
                    STEP(G, c, d, a, b, GET(11), 0x265e5a51, 14)
                    STEP(G, b, c, d, a, GET(0), 0xe9b6c7aa, 20)
                    STEP(G, a, b, c, d, GET(5), 0xd62f105d, 5)
                    STEP(G, d, a, b, c, GET(10), 0x02441453, 9)
                    STEP(G, c, d, a, b, GET(15), 0xd8a1e681, 14)
                    STEP(G, b, c, d, a, GET(4), 0xe7d3fbc8, 20)
                    STEP(G, a, b, c, d, GET(9), 0x21e1cde6, 5)
                    STEP(G, d, a, b, c, GET(14), 0xc33707d6, 9)
                    STEP(G, c, d, a, b, GET(3), 0xf4d50d87, 14)
                    STEP(G, b, c, d, a, GET(8), 0x455a14ed, 20)
                    STEP(G, a, b, c, d, GET(13), 0xa9e3e905, 5)
                    STEP(G, d, a, b, c, GET(2), 0xfcefa3f8, 9)
                    STEP(G, c, d, a, b, GET(7), 0x676f02d9, 14)
                    STEP(G, b, c, d, a, GET(12), 0x8d2a4c8a, 20)

                    // Round 3
                    STEP(H, a, b, c, d, GET(5), 0xfffa3942, 4)
                    STEP(H, d, a, b, c, GET(8), 0x8771f681, 11)
                    STEP(H, c, d, a, b, GET(11), 0x6d9d6122, 16)
                    STEP(H, b, c, d, a, GET(14), 0xfde5380c, 23)
                    STEP(H, a, b, c, d, GET(1), 0xa4beea44, 4)
                    STEP(H, d, a, b, c, GET(4), 0x4bdecfa9, 11)
                    STEP(H, c, d, a, b, GET(7), 0xf6bb4b60, 16)
                    STEP(H, b, c, d, a, GET(10), 0xbebfbc70, 23)
                    STEP(H, a, b, c, d, GET(13), 0x289b7ec6, 4)
                    STEP(H, d, a, b, c, GET(0), 0xeaa127fa, 11)
                    STEP(H, c, d, a, b, GET(3), 0xd4ef3085, 16)
                    STEP(H, b, c, d, a, GET(6), 0x04881d05, 23)
                    STEP(H, a, b, c, d, GET(9), 0xd9d4d039, 4)
                    STEP(H, d, a, b, c, GET(12), 0xe6db99e5, 11)
                    STEP(H, c, d, a, b, GET(15), 0x1fa27cf8, 16)
                    STEP(H, b, c, d, a, GET(2), 0xc4ac5665, 23)

                    // Round 4
                    STEP(I, a, b, c, d, GET(0), 0xf4292244, 6)
                    STEP(I, d, a, b, c, GET(7), 0x432aff97, 10)
                    STEP(I, c, d, a, b, GET(14), 0xab9423a7, 15)
                    STEP(I, b, c, d, a, GET(5), 0xfc93a039, 21)
                    STEP(I, a, b, c, d, GET(12), 0x655b59c3, 6)
                    STEP(I, d, a, b, c, GET(3), 0x8f0ccc92, 10)
                    STEP(I, c, d, a, b, GET(10), 0xffeff47d, 15)
                    STEP(I, b, c, d, a, GET(1), 0x85845dd1, 21)
                    STEP(I, a, b, c, d, GET(8), 0x6fa87e4f, 6)
                    STEP(I, d, a, b, c, GET(15), 0xfe2ce6e0, 10)
                    STEP(I, c, d, a, b, GET(6), 0xa3014314, 15)
                    STEP(I, b, c, d, a, GET(13), 0x4e0811a1, 21)
                    STEP(I, a, b, c, d, GET(4), 0xf7537e82, 6)
                    STEP(I, d, a, b, c, GET(11), 0xbd3af235, 10)
                    STEP(I, c, d, a, b, GET(2), 0x2ad7d2bb, 15)
                    STEP(I, b, c, d, a, GET(9), 0xeb86d391, 21)

                    internal_state[0] = a + internal_state[0];
                    internal_state[1] = b + internal_state[1];
                    internal_state[2] = c + internal_state[2];
                    internal_state[3] = d + internal_state[3];
                }

                void md5(const char* restrict msg, uint length_bytes, uint* restrict out) {
                    uint i;
                    uint bytes_left;
                    char key[64];

                    out[0] = 0x67452301;
                    out[1] = 0xefcdab89;
                    out[2] = 0x98badcfe;
                    out[3] = 0x10325476;

                    for (bytes_left = length_bytes;  bytes_left >= 64; bytes_left -= 64, msg = &msg[64]) {
                        md5_round(out, (const uint*) msg);
                    }

                    for (i = 0; i < bytes_left; i++) {
                        key[i] = msg[i];
                    }
                    key[bytes_left++] = 0x80;

                    if (bytes_left <= 56) {
                        for (i = bytes_left; i < 56; i++) {
                            key[i] = 0;
                        }
                    } else {
                        // If we have to pad enough to roll past this round.
                        for (i = bytes_left; i < 64; i++) {
                            key[i] = 0;
                        }
                        md5_round(out, (uint*) key);
                        for (i = 0; i < 56; i++) {
                            key[i] = 0;
                        }
                    }

                    ulong* len_ptr = (ulong*) &key[56];
                    *len_ptr = length_bytes * 8;
                    md5_round(out, (uint*) key);

                }

                void md5_buffer(const buffer_t* in, buffer_t* out) {
                    md5(in->buffer, in->size, (uint*) out->buffer);
                    out->size = 16;
                }

                static void repeat_md5(buffer_t* buf) {
                    buffer_t local_buf;

                    int i;
                    for (i = 0; i < 25; i++) {
                        md5_buffer(buf, &local_buf);
                        md5_buffer(&local_buf, buf);
                    }
                }
*/
/*
                __kernel void do_md5s(__global char* size, __global char* in, __global char* out) {
                    uint X[5];
                    int id = get_global_id(0);
                    int oc = 0;
                    int a = 0;
                    int carry = 0;
                    X[0] = 0;
                    X[1] = 0;
                    X[2] = 0;
                    X[3] = 0;
                    X[4] = 0;

	                for (int i = 0; i < size[0]; ++i) {
                        oc = id / size[0];
                        a = in[i] + carry + id - oc * size[0];
                        if (a >= size[0]) {
                            a -= size[0];
                            carry = 1;
                        }
                        else {
                            carry = 0;
                        }
                        X[i >> 2] |= in[a] << ((i & 3) << 3);
                        id = oc;
                    }

                    X[size[0] >> 2] |= ((uint)(0x00000080) << ((size[0] & 3) << 3));
	
                    uint A, B, C, D;
	
                    #define S(x,n) ((x << n) | ((x & 0xFFFFFFFF) >> (32 - n)))

                    #define P(a,b,c,d,k,s,t)										\
                    {																\
                        a += F(b,c,d) + X[k] + t; a = S(a,s) + b;					\
                    }																\

                    #define P0(a,b,c,d,k,s,t)										\
                    {																\
                        a += F(b,c,d) + t; a = S(a,s) + b;							\
                    }																\

                    #define P14(a,b,c,d,k,s,t)										\
                    {																\
                        a += F(b,c,d) + (size[0] << 3) + t; a = S(a,s) + b;	\
                    }																\

                    A = 0x67452301;
                    B = 0xefcdab89;
                    C = 0x98badcfe;
                    D = 0x10325476;
	
                    #define F(x,y,z) (z ^ (x & (y ^ z)))

                    P( A, B, C, D,  0,  7, 0xD76AA478 );
                    P( D, A, B, C,  1, 12, 0xE8C7B756 );
                    P( C, D, A, B,  2, 17, 0x242070DB );
                    P( B, C, D, A,  3, 22, 0xC1BDCEEE );
                    P( A, B, C, D,  4,  7, 0xF57C0FAF );
                    P0( D, A, B, C,  5, 12, 0x4787C62A );
                    P0( C, D, A, B,  6, 17, 0xA8304613 );
                    P0( B, C, D, A,  7, 22, 0xFD469501 );
                    P0( A, B, C, D,  8,  7, 0x698098D8 );
                    P0( D, A, B, C,  9, 12, 0x8B44F7AF );
                    P0( C, D, A, B, 10, 17, 0xFFFF5BB1 );
                    P0( B, C, D, A, 11, 22, 0x895CD7BE );
                    P0( A, B, C, D, 12,  7, 0x6B901122 );
                    P0( D, A, B, C, 13, 12, 0xFD987193 );
                    P14( C, D, A, B, 14, 17, 0xA679438E );
                    P0( B, C, D, A, 15, 22, 0x49B40821 );

                    #undef F

                    #define F(x,y,z) (y ^ (z & (x ^ y)))

                    P( A, B, C, D,  1,  5, 0xF61E2562 );
                    P0( D, A, B, C,  6,  9, 0xC040B340 );
	                P0( C, D, A, B, 11, 14, 0x265E5A51 );
                    P( B, C, D, A,  0, 20, 0xE9B6C7AA );
                    P0( A, B, C, D,  5,  5, 0xD62F105D );
                    P0( D, A, B, C, 10,  9, 0x02441453 );
                    P0( C, D, A, B, 15, 14, 0xD8A1E681 );
                    P( B, C, D, A,  4, 20, 0xE7D3FBC8 );
                    P0( A, B, C, D,  9,  5, 0x21E1CDE6 );
                    P14( D, A, B, C, 14,  9, 0xC33707D6 );
                    P( C, D, A, B,  3, 14, 0xF4D50D87 );
                    P0( B, C, D, A,  8, 20, 0x455A14ED );
                    P0( A, B, C, D, 13,  5, 0xA9E3E905 );
                    P( D, A, B, C,  2,  9, 0xFCEFA3F8 );
                    P0( C, D, A, B,  7, 14, 0x676F02D9 );
                    P0( B, C, D, A, 12, 20, 0x8D2A4C8A );

                    #undef F
    
                    #define F(x,y,z) (x ^ y ^ z)

                    P0( A, B, C, D,  5,  4, 0xFFFA3942 );
                    P0( D, A, B, C,  8, 11, 0x8771F681 );
                    P0( C, D, A, B, 11, 16, 0x6D9D6122 );
                    P14( B, C, D, A, 14, 23, 0xFDE5380C );
                    P( A, B, C, D,  1,  4, 0xA4BEEA44 );
                    P( D, A, B, C,  4, 11, 0x4BDECFA9 );
                    P0( C, D, A, B,  7, 16, 0xF6BB4B60 );
                    P0( B, C, D, A, 10, 23, 0xBEBFBC70 );
                    P0( A, B, C, D, 13,  4, 0x289B7EC6 );
                    P( D, A, B, C,  0, 11, 0xEAA127FA );
                    P( C, D, A, B,  3, 16, 0xD4EF3085 );
                    P0( B, C, D, A,  6, 23, 0x04881D05 );
                    P0( A, B, C, D,  9,  4, 0xD9D4D039 );
                    P0( D, A, B, C, 12, 11, 0xE6DB99E5 );
                    P0( C, D, A, B, 15, 16, 0x1FA27CF8 );
                    P( B, C, D, A,  2, 23, 0xC4AC5665 );

                    #undef F

                    #define F(x,y,z) (y ^ (x | ~z))

                    P( A, B, C, D,  0,  6, 0xF4292244 );
                    P0( D, A, B, C,  7, 10, 0x432AFF97 );
                    P14( C, D, A, B, 14, 15, 0xAB9423A7 );
                    P0( B, C, D, A,  5, 21, 0xFC93A039 );
                    P0( A, B, C, D, 12,  6, 0x655B59C3 );
                    P( D, A, B, C,  3, 10, 0x8F0CCC92 );
                    P0( C, D, A, B, 10, 15, 0xFFEFF47D );
                    P( B, C, D, A,  1, 21, 0x85845DD1 );
                    P0( A, B, C, D,  8,  6, 0x6FA87E4F );
                    P0( D, A, B, C, 15, 10, 0xFE2CE6E0 );
                    P0( C, D, A, B,  6, 15, 0xA3014314 );
                    P0( B, C, D, A, 13, 21, 0x4E0811A1 );
                    P( A, B, C, D,  4,  6, 0xF7537E82 );
                    P0( D, A, B, C, 11, 10, 0xBD3AF235 );
                    P( C, D, A, B,  2, 15, 0x2AD7D2BB );
                    P0( B, C, D, A,  9, 21, 0xEB86D391 );

                    #undef F
	
	                hashes[id].x = A + 0x67452301;
	                hashes[id].y = B + 0xefcdab89;
	                hashes[id].z = C + 0x98badcfe;
	                hashes[id].w = D + 0x10325476;

                    out[0] = hashes[id].x;

                }
*/
/*
                __kernel void do_md5s(__global char* size, __global char* in, __global char* out) {
                    int id = get_global_id(0);
                    int id2 = get_global_id(1);
                    int id3 = get_global_id(2);

                    for (int i = 0; i < size[0]; i++) {
                        out[i] = in[i];
                    }

                    //global password_hash_t* outhash = { {0}, {0}, {0}, {0} };
                    //md5(&in, size[0], &outhash);

                    //for (int i = 0; i < 4; i++) {
                    //    out[i] = outhash->v[i];
                    //}
                }
*/

                __kernel void do_md5s(__global char* size, __global char* in, __global char* out) {

                    // Macros for reading/writing chars from int32's (from rar_kernel.cl) 
                    #define GETCHAR(buf, index) (((uchar*)(buf))[(index)])
                    #define PUTCHAR(buf, index, val) (buf)[(index)>>2] = ((buf)[(index)>>2] & ~(0xffU << (((index) & 3) << 3))) + ((val) << (((index) & 3) << 3))

                    // The basic MD5 functions
                    #define F(x, y, z)			((z) ^ ((x) & ((y) ^ (z))))
                    #define G(x, y, z)			((y) ^ ((z) & ((x) ^ (y))))
                    #define H(x, y, z)			((x) ^ (y) ^ (z))
                    #define I(x, y, z)			((y) ^ ((x) | ~(z)))

                    // The MD5 transformation for all four rounds.
                    #define STEP(f, a, b, c, d, x, t, s) \
                        (a) += f((b), (c), (d)) + (x) + (t); \
                        (a) = (((a) << (s)) | (((a) & 0xffffffff) >> (32 - (s)))); \
                        (a) += (b);

                    #define GET(i) (key[(i)])

                    int id = get_global_id(0);
	                uint key[16] = { 0 };
	                uint i;
	                uint num_keys = 1;
	                uint KEY_LENGTH = size[0];

                    for (int n = 0; n < size[0]; n++){
                        key[n] = in[n];
                    }

	                int base = id * (KEY_LENGTH / 4);

	                // padding code (borrowed from MD5_eq.c)
	                char *p = (char *) key;
	                for (i = 0; i != 64 && p[i]; i++);

                    PUTCHAR(key, i, 0x80);
                    PUTCHAR(key, 56, i << 3);
                    PUTCHAR(key, 57, i >> 5);

	                uint a, b, c, d;
	                a = 0x67452301;
	                b = 0xefcdab89;
	                c = 0x98badcfe;
	                d = 0x10325476;

                    // Round 1
                    STEP(F, a, b, c, d, GET(0), 0xd76aa478, 7)
	                STEP(F, d, a, b, c, GET(1), 0xe8c7b756, 12)
	                STEP(F, c, d, a, b, GET(2), 0x242070db, 17)
	                STEP(F, b, c, d, a, GET(3), 0xc1bdceee, 22)
	                STEP(F, a, b, c, d, GET(4), 0xf57c0faf, 7)
	                STEP(F, d, a, b, c, GET(5), 0x4787c62a, 12)
	                STEP(F, c, d, a, b, GET(6), 0xa8304613, 17)
	                STEP(F, b, c, d, a, GET(7), 0xfd469501, 22)
	                STEP(F, a, b, c, d, GET(8), 0x698098d8, 7)
	                STEP(F, d, a, b, c, GET(9), 0x8b44f7af, 12)
	                STEP(F, c, d, a, b, GET(10), 0xffff5bb1, 17)
	                STEP(F, b, c, d, a, GET(11), 0x895cd7be, 22)
	                STEP(F, a, b, c, d, GET(12), 0x6b901122, 7)
	                STEP(F, d, a, b, c, GET(13), 0xfd987193, 12)
	                STEP(F, c, d, a, b, GET(14), 0xa679438e, 17)
	                STEP(F, b, c, d, a, GET(15), 0x49b40821, 22)

                    // Round 2
                    STEP(G, a, b, c, d, GET(1), 0xf61e2562, 5)
	                STEP(G, d, a, b, c, GET(6), 0xc040b340, 9)
	                STEP(G, c, d, a, b, GET(11), 0x265e5a51, 14)
	                STEP(G, b, c, d, a, GET(0), 0xe9b6c7aa, 20)
	                STEP(G, a, b, c, d, GET(5), 0xd62f105d, 5)
	                STEP(G, d, a, b, c, GET(10), 0x02441453, 9)
	                STEP(G, c, d, a, b, GET(15), 0xd8a1e681, 14)
	                STEP(G, b, c, d, a, GET(4), 0xe7d3fbc8, 20)
	                STEP(G, a, b, c, d, GET(9), 0x21e1cde6, 5)
	                STEP(G, d, a, b, c, GET(14), 0xc33707d6, 9)
	                STEP(G, c, d, a, b, GET(3), 0xf4d50d87, 14)
	                STEP(G, b, c, d, a, GET(8), 0x455a14ed, 20)
	                STEP(G, a, b, c, d, GET(13), 0xa9e3e905, 5)
	                STEP(G, d, a, b, c, GET(2), 0xfcefa3f8, 9)
	                STEP(G, c, d, a, b, GET(7), 0x676f02d9, 14)
	                STEP(G, b, c, d, a, GET(12), 0x8d2a4c8a, 20)

                    // Round 3
                    STEP(H, a, b, c, d, GET(5), 0xfffa3942, 4)
	                STEP(H, d, a, b, c, GET(8), 0x8771f681, 11)
	                STEP(H, c, d, a, b, GET(11), 0x6d9d6122, 16)
	                STEP(H, b, c, d, a, GET(14), 0xfde5380c, 23)
	                STEP(H, a, b, c, d, GET(1), 0xa4beea44, 4)
	                STEP(H, d, a, b, c, GET(4), 0x4bdecfa9, 11)
	                STEP(H, c, d, a, b, GET(7), 0xf6bb4b60, 16)
	                STEP(H, b, c, d, a, GET(10), 0xbebfbc70, 23)
	                STEP(H, a, b, c, d, GET(13), 0x289b7ec6, 4)
	                STEP(H, d, a, b, c, GET(0), 0xeaa127fa, 11)
	                STEP(H, c, d, a, b, GET(3), 0xd4ef3085, 16)
	                STEP(H, b, c, d, a, GET(6), 0x04881d05, 23)
	                STEP(H, a, b, c, d, GET(9), 0xd9d4d039, 4)
	                STEP(H, d, a, b, c, GET(12), 0xe6db99e5, 11)
	                STEP(H, c, d, a, b, GET(15), 0x1fa27cf8, 16)
	                STEP(H, b, c, d, a, GET(2), 0xc4ac5665, 23)

                    // Round 4
                    STEP(I, a, b, c, d, GET(0), 0xf4292244, 6)
	                STEP(I, d, a, b, c, GET(7), 0x432aff97, 10)
	                STEP(I, c, d, a, b, GET(14), 0xab9423a7, 15)
	                STEP(I, b, c, d, a, GET(5), 0xfc93a039, 21)
	                STEP(I, a, b, c, d, GET(12), 0x655b59c3, 6)
	                STEP(I, d, a, b, c, GET(3), 0x8f0ccc92, 10)
	                STEP(I, c, d, a, b, GET(10), 0xffeff47d, 15)
	                STEP(I, b, c, d, a, GET(1), 0x85845dd1, 21)
	                STEP(I, a, b, c, d, GET(8), 0x6fa87e4f, 6)
	                STEP(I, d, a, b, c, GET(15), 0xfe2ce6e0, 10)
	                STEP(I, c, d, a, b, GET(6), 0xa3014314, 15)
	                STEP(I, b, c, d, a, GET(13), 0x4e0811a1, 21)
	                STEP(I, a, b, c, d, GET(4), 0xf7537e82, 6)
	                STEP(I, d, a, b, c, GET(11), 0xbd3af235, 10)
	                STEP(I, c, d, a, b, GET(2), 0x2ad7d2bb, 15)
	                STEP(I, b, c, d, a, GET(9), 0xeb86d391, 21)
            
                    out[3] = (a + 0x67452301) >> 24;
                    out[2] = ((a + 0x67452301) << 8) >> 24;
                    out[1] = ((a + 0x67452301) << 16) >> 24;
                    out[0] = ((a + 0x67452301) << 24) >> 24;
	                out[7] = (b + 0xefcdab89) >> 24;
	                out[6] = ((b + 0xefcdab89) << 8) >> 24;
	                out[5] = ((b + 0xefcdab89) << 16) >> 24;
	                out[4] = ((b + 0xefcdab89) << 24) >> 24;
	                out[11] = (c + 0x98badcfe)  >> 24;
	                out[10] = ((c + 0x98badcfe) << 8) >> 24;
	                out[9] = ((c + 0x98badcfe) << 16) >> 24;
	                out[8] = ((c + 0x98badcfe) << 24) >> 24;
	                out[15] = (d + 0x10325476) >> 24;
	                out[14] = ((d + 0x10325476) << 8) >> 24;
	                out[13] = ((d + 0x10325476) << 16) >> 24;
	                out[12] = ((d + 0x10325476) << 24) >> 24;
                }

             __kernel void blur(read_only image2d_t source, write_only image2d_t destination) {
                // Get pixel coordinate
                int2 coord = (int2)(get_global_id(0), get_global_id(1));

                // Create a sampler that use edge color for coordinates outside the image
                const sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

                // Blur using colors in a 7x7 square
                uint4 color = (uint4)(0, 0, 0, 0);
                for(int u=-3; u<=3; u++) {
                    for(int v=-3; v<=3; v++) {
                        color += read_imageui(source, sampler, coord + (int2)(u, v));
                    }
                }
                color /= 49;

                // Write blurred pixel in destination image
                write_imageui(destination, coord, color);
             }

            __kernel void invert(read_only image2d_t source, write_only image2d_t destination) {
                // Get pixel coordinate
                int2 coord = (int2)(get_global_id(0), get_global_id(1));

                // Read color ad invert it (except for alpha value)
                uint4 color = read_imageui(source, coord);
                color.xyz = (uint3)(255,255,255) - color.xyz;

                // Write inverted pixel in destination image
                write_imageui(destination, coord, color);
             }

            __kernel void copy(read_only image2d_t input, write_only image2d_t output, float foo) {
                int2 coord = (int2)(get_global_id(0), get_global_id(1));
                write_imagef(output, coord, read_imagef(input, coord) + foo);
             }

        ";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Get a context for the first GPU platform found
            context = Context.AutomaticContext(DeviceType.GPU);
            if (context == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "No OpenCL compatible GPU found!",
                    Content = "Please install or update you GPU driver and retry.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
            else
            {
                String plats = "";
                foreach (Platform p in Platform.GetPlatforms())
                {
                    plats += "Name:\t" + p.Name + "\nVendor:\t" + p.Vendor + "\nVersion:\t" + p.Version + "\n";
                }
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "GPU found!",
                    Content = plats,
                    CloseButtonText = "Close"
                };
                await dialog.ShowAsync();
            }
            
            // Get a command queue for the first available device in the context
            commandQueue = context.CreateCommandQueue();
            if(commandQueue == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't create a command queue for the current context.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            // Build all kernels from source code
            kernels = context.BuildAllKernels(kernelsCode);
            if(context.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't compile kernels, please check source code.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    WriteableBitmap writeableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    writeableBitmap.SetSource(fileStream);
                    SourceImage.Source = writeableBitmap;

                    BlurButton.IsEnabled = true;
                    InvertButton.IsEnabled = true;
                }
            }
            else
            {
                
                var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/8/8e/Xbox_Velocity_Architecture_branding.jpg";
                var client = new HttpClient();
                Stream stream = await client.GetStreamAsync(imageUrl);
                var memStream = new MemoryStream();
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;
                // since I'm hardcoding a url for an image, I might as well hardcode the dimensions of the image
                WriteableBitmap writeableBitmap = new WriteableBitmap(1920, 1080);
                writeableBitmap.SetSource(memStream.AsRandomAccessStream());
                SourceImage.Source = writeableBitmap;

                BlurButton.IsEnabled = true;
                InvertButton.IsEnabled = true;                
            }
        }

        private void Blur_Click(object sender, RoutedEventArgs e)
        {
            ExecuteKernel("blur");
        }

        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            ExecuteKernel("invert");
        }
        private void MD5_Click(object sender, RoutedEventArgs e)
        {
            ExecuteKernel_Hash("do_md5s");
        }

        private async void ExecuteKernel_Hash(string kernelName)
        {
            byte[] tmp = Encoding.UTF8.GetBytes("hashcat");
            SharpCL.Buffer srcBuffer = context.CreateBuffer(tmp, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer);
            //SharpCL.Image srcImg = context.CreateImage1DBuffer(srcBuffer, tmp, (ulong)tmp.Length, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer, ImageChannelOrder.RGB, ImageChannelType.SignedInt16);

            if (srcBuffer == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "srcBuffer",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
            byte[] size = { (byte)tmp.Length, 0 };
            SharpCL.Buffer sizeBuffer = context.CreateBuffer(size, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer);
            if (sizeBuffer == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "sizeBuffer",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            byte[] tmp2 = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAAAA");
            SharpCL.Buffer dstBuffer = context.CreateBuffer<char>((ulong)16, MemoryFlags.WriteOnly );
            //SharpCL.Image dstImg = context.CreateImage1DBuffer(dstBuffer, tmp, (ulong)tmp.Length, MemoryFlags.WriteOnly | MemoryFlags.CopyHostPointer, ImageChannelOrder.RGB, ImageChannelType.SignedInt16);
            
            if (dstBuffer == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "dstBuffer",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }


            kernels[kernelName].SetArgument(0, sizeBuffer);
            kernels[kernelName].SetArgument(1, srcBuffer);
            kernels[kernelName].SetArgument(2, dstBuffer);

            //Event kernelEvent = commandQueue.EnqueueKernel(kernels[kernelName], new ulong[] { (ulong)16 }, new ulong[] { (ulong)0 }, new ulong[] { (ulong)8 });
            Event kernelEvent = commandQueue.EnqueueKernel(kernels[kernelName], new ulong[] { (ulong)16 });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue kernel on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }

            // Read destination data; fails due to null buffer
            byte[] destinationData = new byte[16];
            commandQueue.EnqueueReadBuffer(dstBuffer, destinationData, true, default, 0, new List<Event> { kernelEvent });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue read buffer command on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }
            string hexString = BitConverter.ToString(destinationData);
            hexString = hexString.Replace("-", "");
            TextBox.Text = hexString;

        }
        private async void ExecuteKernel(string kernelName)
        {
            // Get source pixel data
            WriteableBitmap sourceBitmap = SourceImage.Source as WriteableBitmap;
            byte[] sourceData = sourceBitmap.PixelBuffer.ToArray();

            
            // Create OpenCL images
            SharpCL.Image test = context.CreateImage1D(1080, MemoryFlags.ReadOnly, ImageChannelOrder.RGB, ImageChannelType.SignedInt32);
            // checking if bug somehow in 2D image only
            // future nic here: the bug in dynamic data types for images (probably all and not just images)
            if (test == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "CreateImage1D",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            /*
             https://github.com/microsoft/OpenCLOn12/blob/master/test/openclon12test.cpp#L120
             cl::Image2D input(context, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
                cl::ImageFormat(CL_RGBA, CL_FLOAT), width, height,
                sizeof(float) * width * 4, InputData);
             */
            SharpCL.Image sourceImage = context.CreateImage2D(sourceData, (ulong)sourceBitmap.PixelWidth, (ulong)sourceBitmap.PixelHeight,
                MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer, ImageChannelOrder.RGBA, ImageChannelType.UnsignedInt8);
            SharpCL.Image destinationImage = context.CreateImage2D((ulong)sourceBitmap.PixelWidth, (ulong)sourceBitmap.PixelHeight, MemoryFlags.WriteOnly, ImageChannelOrder.RGBA, ImageChannelType.UnsignedInt8);

            if (sourceImage == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Source Image",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            if (destinationImage == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Destination Image",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            // Run blur kernel
            kernels[kernelName].SetArgument(0, sourceImage);
            kernels[kernelName].SetArgument(1, destinationImage);
            Event kernelEvent = commandQueue.EnqueueKernel(kernels[kernelName], new ulong[] { (ulong)sourceBitmap.PixelWidth, (ulong)sourceBitmap.PixelHeight });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue kernel on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }

            // Read destination image data
            byte[] destinationData = new byte[sourceBitmap.PixelWidth * sourceBitmap.PixelHeight * 4];
            commandQueue.EnqueueReadImage(destinationImage, destinationData, default, default, true, new List<Event> { kernelEvent });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue read image command on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }

            // Use data to create a bitmap source for DestinationImage
            WriteableBitmap writeableBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight);
            using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(destinationData, 0, sourceBitmap.PixelWidth * sourceBitmap.PixelHeight * 4);
            }
            DestinationImage.Source = writeableBitmap;

        }

    }
}
