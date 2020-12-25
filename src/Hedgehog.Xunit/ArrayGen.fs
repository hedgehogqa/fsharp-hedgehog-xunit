module internal ArrayGen

open System
open Hedgehog

let toGenTuple = function
  | [||] -> failwith "The test method must take at least one parameter."
  | [|a|] -> gen {
    let! a = a
    return (Tuple.Create a) |> box }
  | [|a;b|] -> gen {
    let! a = a
    let! b = b
    return (a,b) |> box }
  | [|a;b;c|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    return (a,b,c) |> box }
  | [|a;b;c;d|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    return (a,b,c,d) |> box }
  | [|a;b;c;d;e|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    return (a,b,c,d,e) |> box }
  | [|a;b;c;d;e;f|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    return (a,b,c,d,e,f) |> box }
  | [|a;b;c;d;e;f;g|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    return (a,b,c,d,e,f,g) |> box }
  | [|a;b;c;d;e;f;g;h|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    return (a,b,c,d,e,f,g,h) |> box }
  | [|a;b;c;d;e;f;g;h;i|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    return (a,b,c,d,e,f,g,h,i) |> box }
  | [|a;b;c;d;e;f;g;h;i;j|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    return (a,b,c,d,e,f,g,h,i,j) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    return (a,b,c,d,e,f,g,h,i,j,k) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    return (a,b,c,d,e,f,g,h,i,j,k,l) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    return (a,b,c,d,e,f,g,h,i,j,k,l,m) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    let! u = u
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u;v|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    let! u = u
    let! v = v
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u;v;w|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    let! u = u
    let! v = v
    let! w = w
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u;v;w;x|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    let! u = u
    let! v = v
    let! w = w
    let! x = x
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u;v;w;x;y|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    let! u = u
    let! v = v
    let! w = w
    let! x = x
    let! y = y
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y) |> box }
  | [|a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u;v;w;x;y;z|] -> gen {
    let! a = a
    let! b = b
    let! c = c
    let! d = d
    let! e = e
    let! f = f
    let! g = g
    let! h = h
    let! i = i
    let! j = j
    let! k = k
    let! l = l
    let! m = m
    let! n = n
    let! o = o
    let! p = p
    let! q = q
    let! r = r
    let! s = s
    let! t = t
    let! u = u
    let! v = v
    let! w = w
    let! x = x
    let! y = y
    let! z = z
    return (a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z) |> box }
  | _ -> failwith "Open an issue if you actually want more."
